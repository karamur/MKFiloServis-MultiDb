using KOAFiloServis.Web.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace KOAFiloServis.Web.Services.AI;

/// <summary>
/// ML.NET tabanlı offline anomali tespiti.
/// Internet gerektirmez, model dosyada saklanır.
/// Puantaj verisinde anormal sefer/mesai pattern'lerini yakalar.
/// </summary>
public class AnomalyDetectionService
{
    private readonly MLContext _ml;
    private readonly string _modelPath;
    private PredictionEngine<PuantajData, PuantajPrediction>? _engine;
    private readonly ILogger<AnomalyDetectionService> _logger;

    public AnomalyDetectionService(IWebHostEnvironment env, ILogger<AnomalyDetectionService> logger)
    {
        _ml = new MLContext(seed: 42);
        _modelPath = Path.Combine(env.ContentRootPath, "Data", "ai-anomaly-model.zip");
        _logger = logger;
        InitializeModel();
    }

    private void InitializeModel()
    {
        if (File.Exists(_modelPath))
        {
            try
            {
                var trainedModel = _ml.Model.Load(_modelPath, out _);
                _engine = _ml.Model.CreatePredictionEngine<PuantajData, PuantajPrediction>(trainedModel);
                _logger.LogInformation("ML model yüklendi: {Path}", _modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ML model yüklenemedi, yeni eğitim yapılacak");
                TrainDefaultModel();
            }
        }
        else
        {
            TrainDefaultModel();
        }
    }

    /// <summary>Varsayılan eğitim verisiyle model oluştur</summary>
    public void TrainDefaultModel()
    {
        var normalData = new List<PuantajData>();

        // Normal pattern'ler: 1-2 sefer, 0-1 mesai, düşük-orta fiyat
        for (int sefer = 1; sefer <= 3; sefer++)
            for (int mesai = 0; mesai <= 2; mesai++)
                for (int fiyat = 50; fiyat <= 300; fiyat += 50)
                    normalData.Add(new PuantajData
                    {
                        ToplamSefer = sefer, Mesai = mesai, EkSefer = 0,
                        BirimFiyat = fiyat, IsAnomaly = 0
                    });

        // Anomali pattern'ler: aşırı sefer (>10), çok yüksek mesai, ekstrem fiyat
        var anomalyData = new List<PuantajData>();
        for (int sefer = 10; sefer <= 20; sefer += 5)
            for (int mesai = 5; mesai <= 10; mesai += 5)
                anomalyData.Add(new PuantajData
                {
                    ToplamSefer = sefer, Mesai = mesai, EkSefer = 0,
                    BirimFiyat = 500, IsAnomaly = 1
                });

        anomalyData.Add(new PuantajData { ToplamSefer = 0, Mesai = 0, EkSefer = 0, BirimFiyat = 10000, IsAnomaly = 1 });
        anomalyData.Add(new PuantajData { ToplamSefer = 31, Mesai = 10, EkSefer = 10, BirimFiyat = 100, IsAnomaly = 1 });

        var allData = normalData.Concat(anomalyData).ToList();
        Train(allData);

        _logger.LogInformation("ML model eğitildi: {Normal} normal + {Anomaly} anomali",
            normalData.Count, anomalyData.Count);
    }

    /// <summary>Verilen veriyle modeli yeniden eğit</summary>
    public void Train(List<PuantajData> data)
    {
        var trainData = _ml.Data.LoadFromEnumerable(data);

        var pipeline = _ml.Transforms.Concatenate("Features",
                nameof(PuantajData.ToplamSefer),
                nameof(PuantajData.Mesai),
                nameof(PuantajData.EkSefer),
                nameof(PuantajData.BirimFiyat))
            .Append(_ml.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(PuantajData.IsAnomaly)));

        var model = pipeline.Fit(trainData);

        var dir = Path.GetDirectoryName(_modelPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _ml.Model.Save(model, trainData.Schema, _modelPath);
        _engine = _ml.Model.CreatePredictionEngine<PuantajData, PuantajPrediction>(model);
    }

    /// <summary>Puantaj verisinde anomali var mı?</summary>
    public (bool IsAnomaly, float Score, string? Reason) Predict(int toplamSefer, int mesai, int ekSefer, decimal birimFiyat)
    {
        if (_engine == null)
            return (false, 0, null);

        var pred = _engine.Predict(new PuantajData
        {
            ToplamSefer = toplamSefer,
            Mesai = mesai,
            EkSefer = ekSefer,
            BirimFiyat = (float)birimFiyat
        });

        string? reason = null;
        if (pred.IsAnomaly)
        {
            if (toplamSefer > 10) reason = "Sefer sayısı anormal yüksek";
            else if (mesai > 5) reason = "Mesai sayısı anormal yüksek";
            else if (birimFiyat > 1000) reason = "Birim fiyat anormal yüksek";
            else reason = "AI anomali tespit etti";
        }

        return (pred.IsAnomaly, pred.Probability, reason);
    }
}
