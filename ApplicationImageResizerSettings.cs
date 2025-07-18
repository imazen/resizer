using ImageResizer.Configuration;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.HybridCache;
using ImageResizer.Plugins.Imageflow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;
using System.Text;
using System.Web.Hosting;
using ImageResizer;

/// <summary>
/// ImageResizer için dinamik ve kod tabanlı yapılandırma sağlar.
/// Bu sınıf, ImageResizer kütüphanesinin tüm ayarlarını ve plugin yüklemelerini
/// web.config dosyasına bağımlı kalmadan, C# kodu üzerinden yönetmek için tasarlanmıştır.
/// Bu yaklaşım, ayarların veritabanı, AppSettings veya başka bir dinamik kaynaktan
/// okunarak çalışma zamanında güncellenebilmesine olanak tanır.
/// </summary>
public static class ApplicationImageResizerSettings
{
    // _isConfigured: Bu bayrak, Configure metodunun uygulama ömrü boyunca (Application_Start olayında)
    // sadece bir kez çalışmasını garantiler. Tekrar tekrar çağrılmasını önler.
    private static bool _isConfigured = false;
    // _lock: Çoklu iş parçacığı (multi-thread) ortamlarında Configure metoduna güvenli erişim sağlamak için
    // kullanılan bir kilit nesnesidir. Bu, aynı anda birden fazla isteğin yapılandırma işlemini tetiklemesini engeller.
    private static readonly object _lock = new object();

    /// <summary>
    /// ImageResizer kütüphanesini başlatır ve yapılandırır.
    /// Bu metot, ASP.NET uygulamasının başlangıç noktası olan Global.asax.cs dosyasındaki
    /// Application_Start olayında yalnızca bir kez çağrılmalıdır.
    /// </summary>
    public static void Configure()
    {
        // Double-checked locking deseni: Yapılandırma zaten yapıldıysa, kilit almadan hemen çık.
        if (_isConfigured) return;

        // Kilit alarak, Configure metodunun aynı anda sadece bir iş parçacığı tarafından çalıştırılmasını sağla.
        lock (_lock)
        {
            // Kilit alındıktan sonra tekrar kontrol et. Bu, birden fazla iş parçacığının
            // ilk kontrolü geçip sıraya girmesi durumunda, yapılandırmanın tekrar tekrar yapılmasını önler.
            if (_isConfigured) return;

            // =================================================================================================
            // BÖLÜM 1: DİNAMİK AYARLARI DIŞ KAYNAKTAN OKUMA
            // =================================================================================================
            // Bu bölümdeki tüm değerler, veritabanı, AppSettings, bir JSON dosyası veya başka bir
            // dinamik kaynaktan okunarak bu metoda beslenmelidir. Bu örnekte, anlaşılırlık için sabit değerler kullanılmıştır.
            // Gerçek bir uygulamada, bu değerler bir yapılandırma servisinden veya veritabanından çekilmelidir.

            // maxTotalMegapixels: İşlenecek *kaynak* görselin sahip olabileceği maksimum megapiksel değeri.
            // Bu ayar, sunucuyu aşırı büyük (örneğin: 100 megapiksel, 500 MB\"lık bir TIFF) dosyaların
            // neden olabileceği RAM ve CPU tüketiminden korumak için kritik bir güvenlik ayarıdır.
            // Değer, genişlik * yükseklik / 1,000,000 olarak hesaplanır.
            // Örnek: 8000x6000 piksel bir resim 48 megapikseldir.
            // Hata Durumu: Bu limit aşıldığında, görsel işlenmez ve genellikle HTTP 404 (Not Found)
            // veya 500 (Internal Server Error) hatası döner. Detaylı hata bilgileri uygulama loglarında görülebilir.
            long maxTotalMegapixels = 45;

            // maxImageWidth: İşlem *sonucunda* üretilecek görselin sahip olabileceği maksimum genişlik.
            // Bu ayar, ImageResizer\"ın çıktı boyutunu sınırlar. Örneğin, URL\"de ?width=9000 gibi bir değer istense bile,
            // bu ayar onu 8000 ile sınırlar. Bu, istemcilerin çok büyük görseller talep etmesini engeller.
            int maxImageWidth = 8000;

            // maxImageHeight: İşlem *sonucunda* üretilecek görselin sahip olabileceği maksimum yükseklik.
            // maxImageWidth ile benzer şekilde, çıktı görselinin maksimum yüksekliğini sınırlar.
            int maxImageHeight = 8000;

            // diagnosticsMode: \"/resizer.debug\" teşhis sayfasının kimler tarafından görüntülenebileceğini belirler.
            // Bu sayfa, yüklü plugin\"leri, yapılandırmayı ve potansiyel sorunları gösterdiği için
            // üretim ortamında herkese açık olmamalıdır. Güvenlik açısından kritik bir ayardır.
            // Olasılıklar:
            // \"localhost\": Sayfaya sadece sunucunun kendisinden (127.0.0.1 veya ::1) yapılan isteklerle erişilebilir.
            //              Bu, üretim ortamları için en güvenli ve tavsiye edilen ayardır.
            // \"allhosts\":  Sayfaya internet üzerinden herkes erişebilir. (Yüksek güvenlik riski taşır!)
            // \"none\":      Teşhis sayfası tamamen devreltilmiş olur. Hiç kimse erişemez.
            string diagnosticsMode = "localhost";

            // licenseKey: Satın alınan lisans anahtarı buraya yapıştırılmalıdır.
            // ImageResizer\"ın lisanslı özelliklerini kullanmak için geçerli bir anahtar gereklidir.
            // Lisans anahtarı 
            // \"R5_\" ile başlamalıdır. Geçersiz bir anahtar, lisanslı özelliklerin çalışmamasına neden olur.
            string licenseKey = "R5_..."; // Gerçek lisans anahtarınızla değiştirin.

            // defaultPipelineCommands: URL\"de özel bir ayar belirtilmediği sürece, tüm görsellere uygulanacak varsayılan komut setidir.
            // Bu, sitenizdeki tüm görseller için tutarlı bir kalite ve optimizasyon standardı sağlar.
            // Örneğin, tüm görsellerin varsayılan olarak belirli bir kalitede (quality=60) ve WebP formatında (webp.quality=30) işlenmesini sağlar.
            // \"&\" karakteri XML içinde \"&amp;\" olarak kaçırılmalıdır.
            // autorotate=false: Görselin EXIF verisindeki yönlendirme bilgisini dikkate almadan döndürmeyi devre dışı bırakır.
            // subsampling=420: JPEG sıkıştırmasında renk alt örneklemesini belirler. 4:2:0, iyi sıkıştırma ve kabul edilebilir kalite dengesi sunar.
            // strip=all: Görselden tüm meta verileri (EXIF, IPTC vb.) kaldırır. Gizliliği artırır ve dosya boyutunu küçültür.
            // jpeg.progressive=true: JPEG görsellerini aşamalı (progressive) olarak kaydeder. Bu, görselin yavaş yüklenen bağlantılarda bile
            //                      tümünün bulanık bir versiyonunun görünmesini sağlar, ardından detaylar yüklenir.
            string defaultPipelineCommands = "quality=60&amp;webp.lossless=100&amp;webp.quality=30&amp;autorotate=false&amp;subsampling=420&amp;strip=all&amp;jpeg.progressive=true";

            // =================================================================================================
            // BÖLÜM 2: XML YAPILANDIRMASINI OLUŞTURMA
            // =================================================================================================
            // Tüm ayarları, ImageResizer\"ın anlayacağı tek bir XML metni olarak birleştiriyoruz.
            // Bu XML, doğrudan web.config\"deki <resizer> bölümünün içeriğine karşılık gelir.
            // ÖNEMLİ: ResizerSection kurucusu sadece <resizer> etiketinin içeriğini bekler. Başka XML etiketleri (configuration, configSections vb.)
            // İÇERMEMELİDİR, aksi takdirde NullReferenceException hatası alınır.

            StringBuilder resizerXmlConfig = new StringBuilder();

            resizerXmlConfig.AppendLine("<resizer>");
            resizerXmlConfig.AppendLine("<sizelimits totalMegapixels=\"" + maxTotalMegapixels + "\" width=\"" + maxImageWidth + "\" height=\"" + maxImageHeight + "\" />");
            resizerXmlConfig.AppendLine("<pipeline defaultCommands=\"" + defaultPipelineCommands + "\" />");
            resizerXmlConfig.AppendLine("<diagnostics enableFor=\"" + diagnosticsMode + "\" />");
            resizerXmlConfig.AppendLine("</resizer>");

            // =================================================================================================
            // BÖLÜM 3: YAPILANDIRMAYI VE PLUGIN\"LERİ YÜKLEME (DOĞRU YÖNTEM)
            // =================================================================================================
            // 1. Adım: XML metninden bir ResizerSection nesnesi oluştur.
            // ResizerSection sınıfının bu kurucusu, verilen XML string\"ini ayrıştırır ve
            // ImageResizer\"ın dahili yapılandırma ağacını (Node nesneleri) oluşturur.
            var resizerSection = new ResizerSection(resizerXmlConfig.ToString());

            // 2. Adım: Bu ResizerSection nesnesi ile yeni bir Config nesnesi yarat.
            // Bu, ImageResizer kütüphanesinin tüm iç bağımlılıklarını (PluginConfig, PipelineConfig, IssueSink vb.)
            // doğru ve sıralı bir şekilde başlatmasını sağlar. Bu yöntem, kütüphanenin tasarımına en uygun yoldur.
            // Config sınıfının kaynak kodunda (resizer-5.1.0-rc01/core/Configuration/Config.cs) bu kurucu mevcuttur.
            var c = new Config(resizerSection);

            // Artık \"c\" (Config) nesnesi tam ve kararlı olduğu için, üzerine diğer plugin\"leri güvenle kurabiliriz.
            // Bu logger, özellikle HybridCachePlugin gibi bazı plugin\"ler için gereklidir.
            ILogger logger = NullLoggerFactory.Instance.CreateLogger("ImageResizer");

            // --- HybridCache için Güvenli ve Uyumlu Önbellek Yolu ---
            // ASP.NET uygulamasının App_Data klasörünün fiziksel yolunu alır.
            string appDataPath = HostingEnvironment.MapPath("~/App_Data");
            // App_Data altında \"cache\" adında bir dizin oluşturur. Bu dizin, HybridCache\"
            // önbellek dosyalarını depolayacağı yerdir. Güvenlik ve erişim izinleri açısından uygun bir konumdur.
            string safeCachePath = Path.Combine(appDataPath, "cache");
            // Eğer önbellek dizini mevcut değilse, oluşturur.
            if (!Directory.Exists(safeCachePath))
            {
                Directory.CreateDirectory(safeCachePath);
            }

            // --- Plugin\"lerin Kurulması ---
            // ImageflowBackendPlugin: Imageflow kütüphanesini kullanarak gelişmiş görüntü işleme yetenekleri sağlar.
            // Bu plugin, yüksek performanslı ve modern görüntü işleme algoritmalarını devreye sokar.
            new ImageflowBackendPlugin().Install(c);

            // HybridCachePlugin: Disk ve bellek tabanlı hibrit önbellekleme sağlar.
            // Bu, sık erişilen görsellerin daha hızlı sunulmasına yardımcı olur.
            // CacheSizeMb: Önbelleğin disk üzerindeki maksimum boyutu (MB). Varsayılan 1024 MB\"dir.
            // WriteQueueMemoryMb: Yazma kuyruğu için ayrılan bellek (MB). Varsayılan 100 MB\"dir.
            var cacheOptions = new HybridCacheOptions(safeCachePath)
            {
                CacheSizeMb = 2048,
                WriteQueueMemoryMb = 128
            };
            new HybridCachePlugin(cacheOptions, logger).Install(c);
            

            // Not: SizeLimiting ve Diagnostic plugin\"leri, yukarıda XML içinde tanımlandığı ve
            // Config kurucusu tarafından otomatik olarak yüklendiği için burada tekrar \"new Diagnostic().Install(c);\"
            // veya \"new SizeLimiting().Install(c);\" şeklinde kurulmasına gerek yoktur.
            // Tekrar kurulmaları, IMultiInstancePlugin arayüzünü uygulamadıkları için hataya neden olabilir
            // ve gereksiz kaynak tüketimine yol açabilir.

            // --- Lisanslama Plugin\"lerinin Kurulması ---
            // StaticLicenseProvider: Lisans anahtarını doğrudan koddan alır ve ImageResizer\"a kaydeder.
            // Bu, lisans anahtarının web.config\"de görünmesini engeller ve daha güvenli bir yöntem sunar.
            if (!string.IsNullOrEmpty(licenseKey) && licenseKey.StartsWith("R5_"))
            {
                new StaticLicenseProvider(licenseKey).Install(c);
            }
            // WebConfigLicenseReader: web.config\"deki <licenses> bölümünden lisansları okur.
            // Bu plugin, hem koddan hem de web.config\"den lisans okunabilmesini sağlar (opsiyonel).
            // Eğer web.config\"de lisans tanımlıysa, bu plugin onu da yükleyecektir.
            new WebConfigLicenseReader().Install(c);

            // =================================================================================================
            // BÖLÜM 4: YAPILANDIRMANIN TAMAMLANDIĞINI İŞARETLEME
            // =================================================================================================
            // Bu bayrağı true yaparak, Configure metodunun uygulama ömrü boyunca bir daha çalışmamasını garantileriz.
            // Bu, gereksiz yeniden yapılandırmaları ve potansiyel hataları önler.
            _isConfigured = true;
        }
    }
}





/*
        var sizeLimitingPlugin = c.Plugins.Get<SizeLimiting>();
        if (sizeLimitingPlugin != null)
        {
            // Bu değerleri veritabanından, bir ayar dosyasından veya başka bir dinamik kaynaktan okuyabilirsiniz.

            // =================================================================================================
            // PARAMETRE: ImageWidth
            // AÇIKLAMA:  İşlem sonucunda oluşturulacak SONUÇ görselin sahip olabileceği maksimum genişliği piksel cinsinden belirler.
            //            Eğer bir kullanıcı '?width=10000' gibi bu limitten daha büyük bir istekte bulunursa,
            //            ImageResizer isteği reddetmez, bunun yerine en-boy oranını koruyarak genişliği bu limite çeker.
            // AMAÇ:      Kullanıcıların absürt boyutlarda görseller oluşturarak sunucuyu yormasını engellemek.
            // =================================================================================================
            sizeLimitingPlugin.Limits.ImageWidth = 8000;

            // =================================================================================================
            // PARAMETRE: ImageHeight
            // AÇIKLAMA:  İşlem sonucunda oluşturulacak SONUÇ görselin sahip olabileceği maksimum yüksekliği piksel cinsinden belirler.
            //            '?height=10000' gibi bir istekte bulunulursa, yükseklik bu değere limitlenir.
            // AMAÇ:      Kullanıcıların absürt boyutlarda görseller oluşturarak sunucuyu yormasını engellemek.
            // =================================================================================================
            sizeLimitingPlugin.Limits.ImageHeight = 8000;

            // =================================================================================================
            // PARAMETRE: TotalMegapixels
            // AÇIKLAMA:  İşleme alınacak KAYNAK görselin sahip olabileceği maksimum toplam piksel sayısını (Genişlik x Yükseklik) belirler.
            //            Bu limit, görsel RAM'e yüklenmeden ÖNCE kontrol edilir. Eğer kaynak görsel bu limiti aşarsa,
            //            ImageResizer bir 'ImageProcessingException' fırlatır ve işlemi hiç başlatmaz.
            // AMAÇ:      Sunucunun belleğini (RAM) tüketebilecek ve sistemi çökertebilecek çok büyük dosyaların
            //            işlenmesini en başından engelleyerek Denial-of-Service (DoS) saldırılarına karşı koruma sağlamak.
            //
            // HATA YAKALAMA ÖRNEĞİ:
            // Bu hatayı, görseli işleyen kod bloğunda bir try-catch ile yakalayabilirsiniz.
            //
            // try
            // {
            //     // Görsel işleme kodunuz, örneğin:
            //     ImageResizer.ImageBuilder.Current.Build("buyuk_gorsel.jpg", "kucuk_gorsel.jpg", "width=200");
            // }
            // catch (ImageResizer.ImageProcessingException ex)
            // {
            //     // Hata mesajı genellikle "The source image is x megapixels, which is larger than the configured limit of y megapixels." şeklinde olur.
            //     if (ex.Message.Contains("megapixels"))
            //     {
            //         // Kullanıcıya "Yüklediğiniz görselin çözünürlüğü çok yüksek, lütfen daha küçük bir görsel yükleyin."
            //         // gibi bir mesaj gösterebilirsiniz.
            //     }
            // }
            // =================================================================================================
            sizeLimitingPlugin.Limits.TotalMegapixels = 45; // Yaklaşık 8000x5625 piksele denk gelir.
        }
*/
