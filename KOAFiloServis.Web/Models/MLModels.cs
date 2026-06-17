using Microsoft.ML.Data;

namespace KOAFiloServis.Web.Models;

/// <summary>ML.NET eğitim/tahmin veri modeli</summary>
public class PuantajData
{
    [LoadColumn(0)] public float ToplamSefer { get; set; }
    [LoadColumn(1)] public float Mesai { get; set; }
    [LoadColumn(2)] public float EkSefer { get; set; }
    [LoadColumn(3)] public float BirimFiyat { get; set; }
    [LoadColumn(4)] public float IsAnomaly { get; set; } // 0=normal, 1=anomaly
}

/// <summary>ML.NET tahmin çıktısı</summary>
public class PuantajPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsAnomaly { get; set; }

    public float Score { get; set; }
    public float Probability { get; set; }
}
