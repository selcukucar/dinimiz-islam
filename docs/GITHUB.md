# GitHub'da yayınlama

GitHub hesabı bağlandıktan sonra depo oluşturma/yükleme ve sürüm yayını yapılabilir. Önerilen depo adı `dinimiz-islam`, görünürlük public, varsayılan dal main. Yüklenecek kök bu WPF kaynak klasörüdür; üst çalışma alanı, work, kişisel SQLite dosyaları ve önceki Qt paketleri yüklenmemelidir.

GitHub Releases sürümü `v1.0.1` için açıklama CHANGELOG.md içindedir. Ekler: kurulum EXE, taşınabilir ZIP, kaynak ZIP, SHA256SUMS.txt. Release yayını indirmeleri herkese açar. Kod ve yayın dosyaları hazır olsa da hesap bağlantısı yapılmadan çevrimiçi yayın yapılmış sayılmaz.

Bağlantı olmadan elle ilerlemek için GitHub'da boş bir depo açın. Bu klasörü git deposu yapıp hesabınızın sağladığı uzak depo adresine main dalını gönderin. Ardından v1.0.1 etiketini gönderin; release.yml iş akışı Windows üzerinde test ve paketleme yapar. GitHub Actions etkin ve iş akışlarının depo içeriğine yazma yetkisi olmalıdır. Alternatif olarak Releases > Draft a new release bölümünde hazır dosyaları ekleyin. Parola veya kişisel erişim anahtarını sohbete yazmayın.
