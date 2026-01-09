using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using rpg_deneme.Core;
using rpg_deneme.Models;

namespace rpg_deneme.UI
{
    public static class ItemDrawer
    {
        public static Image DrawItem(ItemInstance item)
        {
            if (item == null) return null;

            Bitmap bmp = new Bitmap(40, 40);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent); // Background handled by Slot

                // Draw based on ItemType
                switch (item.ItemType)
                {
                    case Enums.ItemType.Weapon:
                        DrawWeapon(g, item);
                        break;
                    case Enums.ItemType.Armor:
                        DrawArmor(g, item);
                        break;
                    case Enums.ItemType.Consumable:
                        DrawConsumable(g, item);
                        break;
                    case Enums.ItemType.Material:
                        DrawMaterial(g, item);
                        break;
                    case Enums.ItemType.BlessingMarble:
                        DrawBlessingMarble(g, item);
                        break;
                    case Enums.ItemType.EnchantItem:
                        DrawEnchantItem(g, item);
                        break;
                    default:
                        // Generic fallback
                        g.FillRectangle(Brushes.Gray, 10, 10, 20, 20);
                        break;
                }
            }
            return bmp;
        }

        private static void DrawBlessingMarble(Graphics g, ItemInstance item)
        {
            // Küre (Mor/Pembe Parlayan)
            // Dış hale
            g.FillEllipse(Brushes.Indigo, 5, 5, 30, 30);
            g.FillEllipse(Brushes.MediumPurple, 8, 8, 24, 24);

            // İç parlama
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(10, 10, 20, 20);
                using (PathGradientBrush pthGrBrush = new PathGradientBrush(path))
                {
                    pthGrBrush.CenterColor = Color.White;
                    pthGrBrush.SurroundColors = new Color[] { Color.MediumPurple };
                    g.FillEllipse(pthGrBrush, 10, 10, 20, 20);
                }
            }

            // Işık yansıması
            g.FillEllipse(Brushes.White, 12, 12, 8, 8);
        }

        private static void DrawEnchantItem(Graphics g, ItemInstance item)
        {
            // Efsun Nesnesi (Yeşil Parşömen)
            // Parşömen kağıdı
            Point[] scrollPoints = {
                new Point(10, 8),
                new Point(30, 8),
                new Point(30, 32),
                new Point(10, 32)
            };
            g.FillPolygon(Brushes.Beige, scrollPoints);
            g.DrawPolygon(Pens.SaddleBrown, scrollPoints);

            // Rulo kısımları (Üst ve alt)
            g.FillRectangle(Brushes.BurlyWood, 8, 6, 24, 4); // Üst
            g.FillRectangle(Brushes.BurlyWood, 8, 30, 24, 4); // Alt

            // Mistik sembol (Yeşil)
            using (Pen magicPen = new Pen(Color.LimeGreen, 2))
            {
                g.DrawLine(magicPen, 15, 15, 25, 25);
                g.DrawLine(magicPen, 25, 15, 15, 25);
                g.DrawEllipse(magicPen, 14, 14, 12, 12);
            }

            // Parıltı
            g.FillEllipse(Brushes.LightGreen, 28, 8, 6, 6);
        }

        private static void DrawWeapon(Graphics g, ItemInstance item)
        {
            // Çapraz çizim: Sol alt -> Sağ üst olması için 45 derece sağa çeviriyoruz

            // Koordinat sistemi transformasyonu
            g.TranslateTransform(20, 20); // Merkeze taşı
            g.RotateTransform(45); // 45 derece sağa çevir
            g.TranslateTransform(-20, -20); // Geri taşı

            // Şimdi düz çizersek çapraz görünecek.
            // Dikey olarak çizelim (Merkezde)

            Color baseColor = GetColorByUpgradeLevel(item.UpgradeLevel);
            Pen pen = new Pen(baseColor, 3);
            Brush brush = new SolidBrush(baseColor);

            // Sınıfa göre şekil
            var charClass = (Enums.CharacterClass)(item.AllowedClass ?? 0);

            switch (charClass)
            {
                case Enums.CharacterClass.Mage:
                    DrawStaff(g, pen, brush, item);
                    break;

                case Enums.CharacterClass.Rogue:
                    DrawDagger(g, pen, brush);
                    break;

                case Enums.CharacterClass.Warrior:
                    DrawSword(g, pen, brush);
                    break;

                default:
                    // Fallback: If AllowedClass is 0, guess from Name
                    string name = (item.Name ?? "").ToLower();
                    if (name.Contains("asa") || name.Contains("staff") || name.Contains("wand"))
                    {
                        DrawStaff(g, pen, brush, item);
                    }
                    else if (name.Contains("hançer") || name.Contains("dagger") || name.Contains("knife") || name.Contains("bıçak"))
                    {
                        DrawDagger(g, pen, brush);
                    }
                    else
                    {
                        // Default to Sword
                        DrawSword(g, pen, brush);
                    }
                    break;
            }
        }

        private static void DrawStaff(Graphics g, Pen pen, Brush brush, ItemInstance item)
        {
            // Staff (Asa)
            // Uzun çubuk + tepede parlayan küre
            g.DrawLine(pen, 20, 38, 20, 8); // Sap

            // Kafa (Küre)
            g.FillEllipse(Brushes.Cyan, 14, 2, 12, 12);
            // Etrafında halka
            g.DrawEllipse(new Pen(Color.Gold, 2), 14, 2, 12, 12);
            // Parlama
            g.FillEllipse(Brushes.White, 16, 4, 4, 4);
        }

        private static void DrawDagger(Graphics g, Pen pen, Brush brush)
        {
            // Dagger (Hançer) - Kavisli ve kısa
            // Ters tutuş veya kısa olduğu için merkeze yakın
            // Jambiya tarzı kavisli namlu
            Color darkMetal = Color.FromArgb(100, 100, 100);
            Brush darkBrush = new SolidBrush(darkMetal);

            // Namlu (Kavisli)
            Point[] blade = {
                new Point(18, 30), // Kabza üstü
                new Point(24, 28), // Geniş kısım
                new Point(22, 15), // Orta
                new Point(12, 5)   // Uç (Sola kavisli)
            };
            g.FillPolygon(darkBrush, blade);

            // Keskin kenar parlaması
            g.DrawLine(new Pen(Color.Silver, 1), 18, 30, 12, 5);

            // Kabza
            g.DrawLine(new Pen(Color.SaddleBrown, 4), 19, 36, 18, 30);
            // Balçak (Küçük)
            g.DrawLine(new Pen(Color.DarkRed, 2), 15, 30, 25, 30);
        }

        private static void DrawSword(Graphics g, Pen pen, Brush brush)
        {
            // Sword (Kılıç) - Düz, simetrik ve uzun
            Point[] swordBlade = {
                new Point(17, 30),
                new Point(23, 30),
                new Point(22, 5),   // Uç Sağ
                new Point(20, 2),   // Uç Tam
                new Point(18, 5)    // Uç Sol
            };
            g.FillPolygon(brush, swordBlade);

            // Blood groove (oluk) - Koyu çizgi
            g.DrawLine(new Pen(Color.DimGray, 2), 20, 28, 20, 8);

            // Kabza (Brown)
            g.DrawLine(new Pen(Color.SaddleBrown, 5), 20, 38, 20, 30);

            // Balçak (Crossguard) - Belirgin ve geniş
            g.DrawLine(new Pen(Color.Gold, 4), 12, 30, 28, 30);

            // Pommel (Topuz)
            g.FillEllipse(Brushes.Gold, 17, 36, 6, 6);

        }

        private static void DrawArmor(Graphics g, ItemInstance item)
        {
            // Zırh çizimi (Gövde) - Daha detaylı
            Color baseColor = Color.Gray;
            Color trimColor = Color.Silver;

            if (item.AllowedClass == (byte)Enums.CharacterClass.Mage)
            {
                baseColor = Color.Indigo;
                trimColor = Color.Gold;
            }
            else if (item.AllowedClass == (byte)Enums.CharacterClass.Rogue)
            {
                baseColor = Color.DarkSlateGray;
                trimColor = Color.Black;
            }
            else if (item.AllowedClass == (byte)Enums.CharacterClass.Warrior)
            {
                baseColor = Color.Maroon;
                trimColor = Color.Silver;
            }

            Brush brush = new SolidBrush(baseColor);
            Pen trimPen = new Pen(trimColor, 2);

            // Gövde Şekli
            Point[] armorBody = {
                new Point(8, 8),   // Sol Omuz Üst
                new Point(15, 8),  // Boyun Sol
                new Point(20, 12), // Boyun Alt (V Yaka)
                new Point(25, 8),  // Boyun Sağ
                new Point(32, 8),  // Sağ Omuz Üst
                new Point(35, 18), // Sağ Kol Ucu
                new Point(30, 20), // Sağ Kol Altı
                new Point(30, 35), // Sağ Bel
                new Point(10, 35), // Sol Bel
                new Point(10, 20), // Sol Kol Altı
                new Point(5, 18)   // Sol Kol Ucu
            };

            g.FillPolygon(brush, armorBody);
            g.DrawPolygon(trimPen, armorBody);

            // Detaylar (Kemer, göğüs arması vb.)
            if (item.AllowedClass == (byte)Enums.CharacterClass.Warrior)
            {
                // Metal Plaka Göğüs
                g.FillRectangle(Brushes.Silver, 15, 15, 10, 10);
                g.DrawRectangle(Pens.Black, 15, 15, 10, 10);
            }
            else if (item.AllowedClass == (byte)Enums.CharacterClass.Mage)
            {
                // Mistik Sembol
                g.DrawEllipse(new Pen(Color.Cyan, 1), 15, 15, 10, 10);
                g.FillEllipse(Brushes.Cyan, 19, 19, 2, 2);
            }
            else if (item.AllowedClass == (byte)Enums.CharacterClass.Rogue)
            {
                // Çapraz Kayışlar
                g.DrawLine(Pens.Black, 12, 12, 28, 28);
                g.DrawLine(Pens.Black, 28, 12, 12, 28);
            }
        }

        private static void DrawConsumable(Graphics g, ItemInstance item)
        {
            // Pot çizimi (Şişe)
            Brush fluidBrush;
            if (item.EffectType == Enums.ItemEffectType.RestoreHP) fluidBrush = Brushes.Red;
            else if (item.EffectType == Enums.ItemEffectType.RestoreMana) fluidBrush = Brushes.Blue;
            else fluidBrush = Brushes.White;

            // Şişe Gövdesi
            g.FillEllipse(fluidBrush, 12, 15, 16, 16);
            // Şişe Boynu
            g.FillRectangle(fluidBrush, 17, 8, 6, 8);
            // Mantar/Kapak
            g.FillRectangle(Brushes.Brown, 16, 5, 8, 3);

            // Şişe parlaması
            g.FillEllipse(Brushes.White, 15, 18, 4, 4);
        }

        private static Color GetColorByUpgradeLevel(int level)
        {
            if (level >= 9) return Color.Red; // Çok güçlü
            if (level >= 7) return Color.Gold;
            if (level >= 5) return Color.Silver;
            return Color.LightGray;
        }

        private static void DrawMaterial(Graphics g, ItemInstance item)
        {
            // Material (Kese/Parça)
            // Kahverengi bir kese
            g.FillEllipse(Brushes.SaddleBrown, 10, 15, 20, 20);
            g.FillRectangle(Brushes.SaddleBrown, 15, 10, 10, 10);
            // Bağcık
            g.DrawLine(new Pen(Color.Tan, 2), 15, 15, 25, 15);
        }
    }
}
