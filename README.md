# Zen Fence - Masaüstü Düzenleyici ☯️

**Geliştirici:** Serkan ŞENTÜRK  
**Telif Hakkı:** © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.  
**Sürüm:** v1.0.0 Portable (Taşınabilir)

[![Download ZenFence](https://img.shields.io/badge/İndir-ZenFence.exe%20(v1.0.0)-blue?style=for-the-badge&logo=windows)](https://github.com/836982756578/Zen/releases/download/v1.0.0/ZenFence.exe)
[![Release](https://img.shields.io/github/v/release/836982756578/Zen?style=for-the-badge&color=green)](https://github.com/836982756578/Zen/releases/tag/v1.0.0)

---

Windows için %100 bağımsız, sıfır kurulum gerektiren, hafif, ultra düşük bellek tüketen (~3.5 MB) profesyonel masaüstü düzenleyici ve canlı klasör portalı.

---

## 🌟 Tüm Özellikler

1. **🖱️ Masaüstüne Çift Tıklamayla Masaüstü Simgelerini Gizleme (Desktop Quick-Hide Icons):**
   * Masaüstünün boş bir alanına (duvar kağıdına) çift tıkladığınızda masaüstündeki simgeler akıcı bir geçişle anında gizlenir ve tertemiz bir duvar kağıdı görünümü elde edilir; tekrar çift tıkladığınızda geri gelir.
   * Sistem tepsisi menüsünden eylem tercihi (Masaüstü Simgeleri, Zen Grupları veya Her İkisi) isteğe göre değiştirilebilir.

2. **🔒 Konumu ve Boyutu Kilitleme (Lock Position):**
   * Düzeninizi sabitlemek için sağ tık menüsünden *"🔒 Konumu ve Boyutu Kilitle"* seçeneğini işaretleyerek grubun farenin yanlışlıkla kaydırmasına karşı kilitlenmesini sağlayabilirsiniz.

3. **🔍 Grup İçi Hızlı Arama & Filtreleme (Ctrl+F):**
   * `Ctrl+F` ile anında arama kutusunu açıp dosyalarınızı ve oyunlarınızı gerçek zamanlı filtreleyebilirsiniz.

3. **🧊 Gerçek Akrilik Buzlu Cam Efekti (Windows Acrylic / Blur Behind):**
   * Windows 10/11 donanım hızlandırmalı derinlik ve arkadaki duvar kağıdını bulanıklaştıran gerçek mat cam efekti.

4. **🔤 Akıllı Sıralama (Sort By):**
   * Simgeleri tek tıkla *İsme Göre (A-Z / Z-A)*, *Değiştirilme Tarihine Göre* veya *Dosya Türüne Göre* sıralama.

5. **👁️ 3 Farklı Görünüm Modu (View Modes):**
   * **Büyük Simgeler (48x48):** Klasik geniş masaüstü görünümü (1:1 kristal netliğinde).
   * **Kompakt Simgeler (32x32):** Orta boy simgeler ile alan tasarrufu.
   * **Liste / Ayrıntı Görünümü:** Çok sayıda dosya içeren klasör portalları için satır satır liste.

6. **📑 Sekmeli Gruplar (Tabbed Groups):**
   * Bir grupta birden fazla sekme oluşturarak `[ 🎮 Oyunlar ] [ 📁 Belgeler ] [ + ]` sekmeleri arasında tek tıkla geçiş yapabilirsiniz.

7. **🎨 Başlık İkonu ve Özel Emoji Seçimi:**
   * Grubun başlığına özel emojiler atayabilirsiniz (🎮, 📁, 🚀, 💼, 🎵, ⭐ vb.).

8. **⚡ Masaüstünü Otomatik Sınıflandırma (Auto-Organize):**
   * Masaüstünüze gelen yeni görselleri, belgeleri ve programları otomatik olarak ilgili Zen gruplarına yerleştirir.

9. **💾 Masaüstü Düzeni Yedekleme & Geri Yükleme (Snapshot):**
   * Sistem tepsisi menüsünden tüm pencerelerin konum ve ayarlarını tek tıkla yedekleyebilir ve geri yükleyebilirsiniz.

10. **📂 Canlı Klasör Portalı (Folder Portal):**
    * Bilgisayarınızdaki herhangi bir klasörün içeriğini gerçek zamanlı (**FileSystemWatcher**) olarak pencere içinde listeler.

11. **⚡ Kenara Yaslanınca Akışkan Otomatik Katlanma (Zen Roll-up):**
    * Ekran kenarına yaslandığında otomatik katlanır, fare geldiğinde pürüzsüzce açılır.

12. **↔️ Serbest Kenar ve Köşe Boyutlandırma:**
    * Sağ, sol, alt kenarlardan ve köşelerden serbestçe boyutlandırılabilir.

13. **⚡ Ultra Düşük Bellek Tüketimi (~3-5 MB):**
    * Yerleşik Windows Çalışma Kümesi budama mekanizması sayesinde sistem kaynaklarını tüketmez.

---

## 🛠️ Yeniden Derleme
```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```
Çıktı dosyaları:
- `ZenFence.exe`
- `Zen.exe`
