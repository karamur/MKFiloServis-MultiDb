## DeepSeek V3 Entegrasyonu

### Genel Bakış

MKFiloServis artık **DeepSeek V3** AI modelini desteklemektedir. DeepSeek V3, son derece güçlü reasoning (düşünme) yeteneklerine sahip açık kaynak bir modeldir.

### İki Kullanım Yöntemi

#### 1. OpenRouter Üzerinden (Önerilen - Kolay)

OpenRouter birden fazla AI modelini tek bir API üzerinden sunar. DeepSeek V3'ü OpenRouter üzerinden kullanmak için:

**appsettings.json:**
```json
"OpenRouter": {
  "ApiKey": "YOUR_OPENROUTER_API_KEY",
  "Model": "deepseek/deepseek-chat",
  "SiteUrl": "http://localhost:5000",
  "SiteName": "MKFiloServis"
}
```

**Kod Örneği:**
```csharp
@inject IOpenRouterService OpenRouterService

private async Task GenerateWithDeepSeek()
{
    var prompt = "Filo yönetimi için maliyet analizi yap";

    await foreach (var chunk in OpenRouterService.StreamChatCompletionAsync(prompt))
    {
        response += chunk;
        StateHasChanged();
    }
}
```

**Avantajları:**
- ✅ Hızlı kurulum
- ✅ Tek API key ile birden fazla model
- ✅ Otomatik fallback ve rate limiting
- ✅ Mevcut OpenRouter kodunu kullanır

#### 2. Doğrudan DeepSeek API (Gelişmiş)

Daha fazla kontrol ve DeepSeek'e özgü özellikler için doğrudan API kullanımı:

**appsettings.json:**
```json
"DeepSeek": {
  "ApiKey": "YOUR_DEEPSEEK_API_KEY",
  "Model": "deepseek-chat",
  "BaseUrl": "https://api.deepseek.com/v1/"
}
```

**Kod Örneği (Streaming):**
```csharp
@inject IDeepSeekService DeepSeekService

private async Task GenerateWithDeepSeek()
{
    var systemPrompt = "Sen bir filo yönetim uzmanısın.";
    var userPrompt = "Araç bakım maliyetlerini nasıl optimize edebilirim?";

    await foreach (var chunk in DeepSeekService.StreamChatCompletionAsync(
        userPrompt, 
        systemPrompt))
    {
        response += chunk;
        StateHasChanged();
    }
}
```

**Kod Örneği (Non-Streaming):**
```csharp
private async Task GetCompleteResponse()
{
    var response = await DeepSeekService.ChatCompletionAsync(
        prompt: "5 araçlık filo için aylık yakıt bütçesi tahmini yap",
        systemPrompt: "Sen bir filo yönetim uzmanısın.");

    Console.WriteLine(response);
}
```

**Avantajları:**
- ✅ DeepSeek'e özgü reasoning özelliklerine tam erişim
- ✅ `reasoning_effort` parametresi (low/medium/high)
- ✅ Reasoning token istatistikleri
- ✅ Prompt caching desteği
- ✅ Daha düşük maliyet (doğrudan API)

### DeepSeek V3 Özellikleri

- **Güçlü Reasoning**: Karmaşık problemlerde adım adım düşünme
- **8K Output Tokens**: Uzun raporlar ve analizler
- **Fast Response**: Optimized inference
- **Cost-Effective**: OpenAI GPT-4'ten çok daha ucuz
- **Prompt Caching**: Tekrarlanan sorularda maliyet tasarrufu

### API Key Alma

**OpenRouter için:**
1. https://openrouter.ai/ adresine git
2. Ücretsiz hesap oluştur
3. API key al ($5 free credit)

**DeepSeek için:**
1. https://platform.deepseek.com/ adresine git
2. Hesap oluştur
3. API key al (500M free tokens!)

### Örnek Kullanım Senaryoları

#### 1. Filo Maliyet Analizi
```csharp
var prompt = @"
Aşağıdaki filo verilerine göre maliyet analizi yap:
- 10 araç
- Aylık ortalama 5000 km
- Yakıt: 45 TL/litre
- Ortalama yakıt tüketimi: 8L/100km

Detaylı rapor hazırla.
";

var response = await DeepSeekService.ChatCompletionAsync(prompt);
```

#### 2. Bakım Planı Önerisi
```csharp
var systemPrompt = "Sen bir araç bakım uzmanısın.";
var userPrompt = $"Plaka: {plaka}, KM: {km}, Son Bakım: {sonBakim}. Bakım önerisi yap.";

await foreach (var chunk in DeepSeekService.StreamChatCompletionAsync(
    userPrompt, 
    systemPrompt))
{
    // Real-time UI update
    suggestionText += chunk;
    StateHasChanged();
}
```

#### 3. Güzergah Optimizasyonu
```csharp
var prompt = @"
Aşağıdaki güzergahları optimize et:
{JsonSerializer.Serialize(guzergahlar)}

Yakıt tasarrufu ve süre açısından en iyi rotayı öner.
";

var analysis = await DeepSeekService.ChatCompletionAsync(prompt);
```

### Gelişmiş Özellikler

#### Reasoning Effort Kontrolü

DeepSeekService.cs'de `reasoning_effort` parametresini ayarlayabilirsiniz:

```csharp
var requestBody = new
{
    model = _model,
    messages = messages.ToArray(),
    stream = true,
    reasoning_effort = "high" // low, medium, high
};
```

- **low**: Hızlı yanıtlar, basit sorular
- **medium**: Dengeli (default)
- **high**: Karmaşık analiz ve derin düşünme gerektiren işlemler

#### Reasoning Content Görüntüleme

DeepSeek'in "düşünme sürecini" görmek için:

```csharp
if (!string.IsNullOrEmpty(choice.Delta?.ReasoningContent))
{
    // DeepSeek'in reasoning adımlarını görüntüle
    Console.WriteLine($"[THINKING]: {choice.Delta.ReasoningContent}");
}
```

### Performans ve Maliyet

| Model | Hız | Maliyet | Reasoning | Output |
|-------|-----|---------|-----------|--------|
| DeepSeek V3 | ⚡⚡⚡ | $ | ⭐⭐⭐⭐⭐ | 8K |
| GPT-4o | ⚡⚡ | $$$ | ⭐⭐⭐⭐ | 16K |
| Claude Opus | ⚡ | $$$$ | ⭐⭐⭐⭐⭐ | 8K |
| Llama 3.2 (Ollama) | ⚡⚡⚡⚡ | FREE | ⭐⭐⭐ | 4K |

### Hata Yönetimi

```csharp
try
{
    var response = await DeepSeekService.ChatCompletionAsync(prompt);
}
catch (HttpRequestException ex)
{
    _logger.LogError("DeepSeek API error: {Error}", ex.Message);
    // Fallback to Ollama or OpenRouter
}
```

### Test

```bash
# Build
dotnet build MKFiloServis.Web

# Test (varsa AI testleri)
dotnet test --filter "Category=AI"
```

### Güvenlik Notları

- ❗ API key'leri asla source control'e eklemeyin
- ✅ Production'da environment variables veya Azure Key Vault kullanın
- ✅ Rate limiting ve retry logic ekleyin
- ✅ Sensitive data'yı AI'ya göndermeden önce maskeleyin

### İleri Adımlar

1. **Multi-Model Strategy**: Basit işlemler için Ollama, karmaşık analizler için DeepSeek
2. **Prompt Templates**: Yaygın senaryolar için hazır promptlar
3. **Response Caching**: Benzer sorular için cache kullanımı
4. **Function Calling**: DeepSeek V3 tool/function calling desteği
5. **Admin Panel**: Model seçimi ve ayarları için UI

### Kaynaklar

- DeepSeek Docs: https://platform.deepseek.com/api-docs/
- OpenRouter DeepSeek: https://openrouter.ai/models/deepseek/deepseek-chat
- Code: `MKFiloServis.Web/Services/DeepSeekService.cs`
