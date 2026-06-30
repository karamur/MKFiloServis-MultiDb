using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;

namespace MKFiloServis.Web.Services.Interfaces;

public interface ICariRiskService
{
    // Risk Özet
    Task<CariRiskOzet> GetRiskOzetAsync();
    
    // Risk Kartları
    Task<List<CariRiskKarti>> GetRiskKartlariAsync(CariRiskFilterParams? filtre = null);
    Task<CariRiskKarti?> GetCariRiskKartiAsync(int cariId);
    
    // Vadesi Geçmiş Faturalar
    Task<List<VadesiGecmisFatura>> GetVadesiGecmisFaturalarAsync(int? cariId = null, int? minGecikmeGunu = null);
    Task<decimal> GetToplamVadesiGecmisBorcAsync();
    
    // Risk Trend
    Task<List<RiskTrendItem>> GetRiskTrendAsync(int aylikDonemSayisi = 12);
    
    // Risk Hesaplama
    Task<int> HesaplaRiskSkoruAsync(int cariId);
    Task RecalculateAllRiskScoresAsync();
    
    // AI Analiz
    Task<string> GetAIRiskAnaliziAsync(int cariId);
    Task<string> GetTopluAIRiskAnaliziAsync();
}




