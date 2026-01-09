using System.Collections.Generic;
using rpg_deneme.Core;
using rpg_deneme.Models;

namespace rpg_deneme.Business;

public static class EnemyFactory
{
    public static List<EnemyModel> CreateEnemies(EnemyModel template, int count, float multiplier)
    {
        List<EnemyModel> list = new List<EnemyModel>();
        for (int i = 0; i < count; i++)
        {
            EnemyModel scaledEnemy = ScaleEnemy(template, multiplier);
            list.Add(scaledEnemy);
        }
        return list;
    }

    public static EnemyModel ScaleEnemy(EnemyModel template, float multiplier)
    {
        bool isRanged = false;
        try
        {
            if (template != null)
            {
                if (template.IsRanged)
                {
                    isRanged = true;
                }
                else if (template.Type == Enums.EnemyType.Ranged)
                {
                    isRanged = true;
                }
                else if (!string.IsNullOrEmpty(template.SpritePath) && template.SpritePath.Trim().ToUpper().Contains("RANGED"))
                {
                    isRanged = true;
                }
            }
        }
        catch
        {
        }
        EnemyModel scaled = new EnemyModel
        {
            EnemyID = template.EnemyID,
            Name = template.Name,
            Level = template.Level,
            MaxHP = (int)((float)template.MaxHP * multiplier),
            CurrentHP = (int)((float)template.MaxHP * multiplier),
            MinDamage = (int)((float)template.MinDamage * multiplier),
            MaxDamage = (int)((float)template.MaxDamage * multiplier),
            ExpReward = (int)((float)template.ExpReward * multiplier),
            GoldReward = (int)((float)template.GoldReward * multiplier),
            SpritePath = template.SpritePath,
            Width = ((template.Width > 0) ? template.Width : 64),
            Height = ((template.Height > 0) ? template.Height : 64),
            Speed = (isRanged ? (3.5f * multiplier) : (3f * multiplier)),
            IsBoss = template.IsBoss,
            Type = template.Type
        };
        scaled.IsRanged = isRanged;
        scaled.AttackRange = (isRanged ? 250 : 50);
        return scaled;
    }
}
