# RPG Game / RPG Challenge Project

*(English description is below the Turkish section)*

## Proje Hakkında

Bu proje, Windows Forms (WinForms), SkiaSharp ve SQLite teknolojileri kullanılarak geliştirilmiş, 2D aksiyon tabanlı bir RPG oyunudur.

**Geliştirici Notları:**
> winforms, skiasharp, sqlite kullanarak yapılmış 2D ufak bir rpg oyunu.
> deneme, challenge amaçlı yapıldı.
> büyük bölümü çeşitli yapay zekalar ile kodlandı.
> kullanılan altyapıdan dolayı bazı optimizasyon kısıtları olabiliyor.
> üstüne eklemeye, geliştirmeye açık ve buna değecek bir proje oldu bence.
> bu proje, bu işlerde acemi ve giriş seviyesinde olduğum için kullanılan yapıları, geliştirme aşamalarını görmek için faydalı oldu.

### Oyun Özellikleri ve Mekanikler

Oyun, klasik RPG mekaniklerini basit ve sade bir aksiyon mekaniği ile birleştirmeyi hedefler:

*   **Savaş Sistemi:** Gerçek zamanlı, yetenek ve temel saldırı tabanlı aksiyon sistemi. Düşmanlara nişan alıp yeteneklerinizi kullanmanız gerekir.
*   **Oyun Modları:**
    *   **Arena (Hayatta Kalma):** Dalgalar halinde gelen düşmanlara karşı hayatta kalma mücadelesi.
    *   **Bölge (Zone) Sistemi:** Farklı zorluk seviyelerine ve temalara sahip bölgelerde keşif ve savaş.
*   **Karakter Gelişimi:**
    *   **Stat Sistemi:** Güç (STR), Çeviklik (DEX), Zeka (INT) gibi temel istatistikleri geliştirme.
    *   **Yetenek Ağacı (Skill Tree):** Aktif ve pasif yetenekler, büyü efektleri (yanma, zehirleme vb.).
*   **Eşya ve Ekonomi:**
    *   **Envanter Yönetimi:** Silahlar, zırhlar ve tüketilebilir eşyalar.
    *   **Efsun ve Geliştirme:** Eşya seviyesini yükseltme ve efsunlama (Enchantment) sistemleri.
    *   **NPC Etkileşimi:** Tüccar, Depocu ve Demirci gibi çeşitli NPC'ler.
*   **Loot Sistemi:** Düşmanlardan elde edilen rastgele loot havuzu. Grade yapısı da sadece loot'a bağlı.

### Teknik Mimari

Proje, katmanlı bir mimari yapı gözetilerek tasarlanmıştır:

*   **UI Layer (Kullanıcı Arayüzü):** Windows Forms kontrolleri üzerine kurulu görsel yapı.
*   **Rendering (Görselleştirme):** Standart WinForms çizimi yerine, yüksek performanslı 2D çizimler için SkiaSharp kütüphanesi kullanılmıştır. Oyun döngüsü ve çizimler bu motor üzerinden yönetilir.
*   **Business Logic (İş Mantığı):** Oyun kurallarının yönetildiği katman.
    *   Manager sınıfları (InventoryManager, SkillManager, CombatManager vb.) oyun durumunu yönetir.
*   **Data Layer (Veri Katmanı):** Oyun verileri (Yetenekler, Eşyalar, Kayıt dosyaları) SQLite veritabanında saklanır.
*   **Models:** Veri tabanı ve iş mantığı arasında taşınan nesne yapıları (Entity'ler).

### Kullanılan Teknolojiler

*   **Dil:** C# (.NET 8.0)
*   **Arayüz:** Windows Forms (WinForms)
*   **Grafik Motoru:** SkiaSharp
*   **Veritabanı:** SQLite
*   **IDE:** Visual Studio, Antigravity

---

## About the Project

This project is a 2D action-based RPG game developed using **Windows Forms (WinForms)**, **SkiaSharp**, and **SQLite**.

**Developer Notes:**
> This is a small 2D RPG game made using winforms, skiasharp, and sqlite.
> It was created as a trial/challenge project.
> A large part of it was coded with the assistance of various AIs.
> Due to the underlying infrastructure, there may be some optimization constraints.
> However, I believe it's a project worth iterating on and developing further.
> Since I am a beginner in this field, this project was very useful for seeing the structures used and the development stages.

### Game Features and Mechanics

The game aims to combine classic RPG mechanics with simple and plain action mechanics:

*   **Combat System:** Real-time, skill and basic attack-based action system. Requires aiming and using your skills.
*   **Game Modes:**
    *   **Arena (Survival):** Fight against waves of enemies to survive.
    *   **Zone System:** Explore and fight in zones with different difficulty levels and themes.
*   **Character Progression:**
    *   **Stat System:** Upgrade base stats like Strength (STR), Dexterity (DEX), Intelligence (INT).
    *   **Skill Tree:** Active and passive skills, spell effects (burning, poison, etc.).
*   **Item & Economy:**
    *   **Inventory Management:** Weapons, armor, and consumables.
    *   **Enchant & Upgrade:** Systems for upgrading item levels and enchanting equipment.
    *   **NPC Interaction:** Interact with various NPCs like Merchant, Storage Keeper, and Blacksmith.
*   **Loot System:** Random loot pool obtained from enemies. The grade structure depends solely on loot.

### Technical Architecture

The project is designed with a layered architecture:

*   **UI Layer:** Visual structure built on Windows Forms controls.
*   **Rendering:** **SkiaSharp** library is used instead of standard WinForms drawing for high-performance 2D rendering. The game loop and graphics are managed through this engine.
*   **Business Logic:** The layer where game rules are managed.
    *   `Manager` classes (InventoryManager, SkillManager, CombatManager, etc.) handle the game state.
*   **Data Layer:** Game data (Skills, Items, Save files) is stored in a **SQLite** database.
*   **Models:** Object structures (Entities) transferred between the database and business logic.

### Technology Stack

*   **Language:** C# (.NET 8.0)
*   **UI Framework:** Windows Forms (WinForms)
*   **Graphics Engine:** SkiaSharp
*   **Database:** SQLite
*   **IDE:** Visual Studio, Antigravity