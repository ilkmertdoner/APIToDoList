# Yapılacaklar Listesi (Task Management System)

Bu proje, ASP.NET Core Web API backend ve JavaScript, HTML, Tailwind CSS frontend ile geliştirilmiş bir görev yönetim sistemidir.

## 🚀 Kurulum ve Çalıştırma

Projeyi kendi bilgisayarınızda eksiksiz bir şekilde ayağa kaldırmak için aşağıdaki adımları sırasıyla izleyin.

### 1. Projeyi Klonlama

Öncelikle projeyi GitHub üzerinden bilgisayarınıza indirin ve proje klasörüne girin:

```bash
git clone [https://github.com/KULLANICI_ADINIZ/PROJE_ADINIZ.git](https://github.com/KULLANICI_ADINIZ/PROJE_ADINIZ.git)
cd PROJE_ADINIZ
```

### 2. Gerekli Ayarlamalar (appsettings.json)
Backend dizinindeki (TaskManagerApi) appsettings.json dosyasını açın. Veritabanı bağlantı cümlenizi, JWT gizli anahtarınızı ve e-posta gönderimi için SMTP (Uygulama Şifresi) bilgilerinizi aşağıdaki gibi doldurun:

```JSON
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskManagerDb;Trusted_Connection=True;"
  },
  "Jwt": {
    "Key": "BURAYA_EN_AZ_32_KARAKTERLI_GIZLI_BIR_ANAHTAR_YAZIN",
    "Issuer": "http://localhost",
    "Audience": "http://localhost"
  },
  "Email": {
    "Adress": "kendi_mailiniz@gmail.com",
    "Pass": "gmail_uygulama_sifreniz"
  }
}
```

### 3. Veritabanı ve Migration İşlemleri
Proje deposunda migration dosyaları bulunmamaktadır. Veritabanını sıfırdan oluşturmak ve tabloları veritabanına işlemek için backend klasörü içindeyken terminalden şu komutları çalıştırın:

```Bash
cd TaskManagerApi
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
```
Daha sonrasında değişiklik yapmak için: 
```Bash
add-migration "migration ismi"
update-database
```
*Not: Bu komutların çalışması için bilgisayarınızda Entity Framework Core CLI araçlarının yüklü olması gerekmektedir. Yüklü değilse önce dotnet tool install --global dotnet-ef komutunu çalıştırın.*

### 4. Sosyal Giriş API Anahtarlarının Alınması (Google & Microsoft)

Uygulamadaki Google ve Microsoft ile giriş özelliklerinin çalışabilmesi için kendi geliştirici hesaplarınızdan API anahtarları almanız ve projeye entegre etmeniz gerekmektedir.

#### 🔵 Google Kimlik Bilgilerini Alma (Frontend İçin)
1. **Google Cloud Console**'a (console.cloud.google.com) gidin ve yeni bir proje oluşturun.
2. Sol menüden **APIs & Services > Credentials (Kimlik Bilgileri)** sayfasına girin.
3. **Create Credentials > OAuth client ID** butonuna tıklayın.
4. Application type (Uygulama türü) olarak **Web application** seçin.
5. **Authorized JavaScript origins** (Yetkili JavaScript kökleri) alanına, frontend uygulamanızın çalıştığı adresi ekleyin (Örn: `http://127.0.0.1:5500`).
6. Oluşturduğunuz **Client ID** değerini kopyalayın.
7. Frontend klasöründeki `auth.js` dosyasını açın ve `google.accounts.id.initialize` içindeki `client_id` alanına bu değeri yapıştırın.

*Not: google-credentials.json dosyasını indirip, proje içine atmanız gerekmektedir.*

#### 🔵 Microsoft Kimlik Bilgilerini Alma (Backend İçin)
1. **Microsoft Azure Portal**'a (portal.azure.com) gidin ve **Microsoft Entra ID** (eski adıyla Azure AD) hizmetini açın.
2. Sol menüden **App registrations (Uygulama kayıtları)** sekmesine girip **New registration (Yeni kayıt)** butonuna tıklayın.
3. Uygulamanıza isim verin. **Redirect URI** bölümünde platform olarak **Web**'i seçin ve backend callback adresinizi girin (Örn: `https://localhost:7133/api/Auth/microsoft-callback`). Kaydedin.
4. Karşınıza çıkan "Overview" (Genel Bakış) sayfasındaki **Application (client) ID** ve **Directory (tenant) ID** değerlerini kopyalayın.
5. Sol menüden **Certificates & secrets (Sertifikalar ve gizli diziler)** bölümüne girin. **New client secret** oluşturun ve üretilen **Value (Değer)** kısmını kopyalayın (Bu değeri sadece bir kez görebilirsiniz).
6. Kopyaladığınız bu 3 değeri backend projesindeki `appsettings.json` dosyasına aşağıdaki gibi ekleyin:

```json
"TenantId": "KOPYALADIGINIZ_TENANT_ID",
"ClientId": "KOPYALADIGINIZ_CLIENT_ID",
"ClientSecret": "KOPYALADIGINIZ_SECRET_VALUE"```

### 4. Projeyi Çalıştırma
##### (Backend):
Veritabanı oluştuktan sonra, terminalde API klasörünün içindeyken projeyi başlatın:

```Bash
dotnet run
```
API varsayılan olarak https://localhost:7133 portunda ayağa kalkacaktır.

##### (Frontend):
Ön uç için herhangi bir sunucu kurmanıza (Node.js, npm vb.) gerek yoktur. Proje klasöründeki login.html veya index.html dosyasını doğrudan tarayıcınızda açarak sistemi hemen kullanmaya başlayabilirsiniz.

### 5. Google Takvim Entegrasyonu (İsteğe Bağlı)
Projedeki görevlerin Google Takviminize otomatik eklenmesi için bir Service Account (Bot) altyapısı kullanılmıştır. Bu özelliği aktif etmek için şu adımları izlemelisiniz:

#### 1. API ve Service Account Kurulumu:

* Google Cloud Console'da yeni bir proje oluşturun ve Google Calendar API'yi aktifleştirin.

* Bir Service Account (Hizmet Hesabı) oluşturun.

* Oluşturduğunuz hesabın "Keys" (Anahtarlar) bölümünden yeni bir JSON anahtarı indirin.

* İndirdiğiniz dosyanın adını google-credentials.json olarak değiştirin ve backend (TaskManagerApi) klasörünün ana dizinine (Program.cs ile aynı yere) kopyalayın.

#### 2. Takvim İzni Verme (Kritik Adım):
Uygulamanın takviminize kayıt atabilmesi için kişisel takviminizi bota açmanız gerekir.

* google-credentials.json dosyasını açın ve içindeki client_email değerini (botun e-posta adresini) kopyalayın.

* Tarayıcıdan Google Takvim'e (calendar.google.com) girin.

* Sol menüden kendi takviminizin yanındaki üç noktaya tıklayıp Ayarlar ve paylaşım'a girin.

* Belirli kişilerle veya gruplarla paylaş bölümüne gelip Kişi ekle butonuna basın.

* Kopyaladığınız bot e-postasını yapıştırın ve izin seviyesini kesinlikle Etkinliklerde değişiklik yapma (Make changes to events) olarak seçip kaydedin.

Artık uygulamadan bitiş tarihi olan bir görev eklediğinizde, bu görev arka planda otomatik olarak Google Takviminize eklenecektir.

### ✨ Özellikler
* Kullanıcı Kayıt ve E-Posta Doğrulama: Geçici token tablosu (EmailTokens) ile güvenli 3 dakikalık e-posta doğrulama süreci.

* JWT Kimlik Doğrulama: Güvenli oturum ve API yetkilendirme yönetimi.

* Görev Yönetimi: Öncelik atama, bitiş tarihi belirleme ve sürükle-bırak ile tamamlandı durumunu değiştirme.

* Dinamik Takvim: Ay geçişli mini takvim ve gün bazlı açılır görev listesi (Modal).

* Alt Görevler: Açıklama alanında -  ile başlayan metinlerin otomatik olarak tıklanabilir kutucuklara (checkbox) dönüşmesi.


* Dark/Light Mode: Tailwind CSS ile entegre karanlık ve aydınlık tema desteği.
