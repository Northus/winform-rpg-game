using System;
using System.Collections.Generic;
using rpg_deneme.Data;
using rpg_deneme.Models;

namespace rpg_deneme.Business;

public class SurvivalManager
{
	private ZoneRepository _zoneRepo = new ZoneRepository();

	private CharacterRepository _charRepo = new CharacterRepository();

	public (int EnemyCount, int MinLevel, int MaxLevel) GetWaveInfo(int wave)
	{
		int count = (wave - 1) % 10 + 1;
		int tier = (wave - 1) / 10;
		int minLvl = tier * 10 + 1;
		int maxLvl = minLvl + 9;
		return (EnemyCount: count, MinLevel: minLvl, MaxLevel: maxLvl);
	}

	public List<EnemyModel> GenerateWaveEnemies(int wave)
	{
		(int EnemyCount, int MinLevel, int MaxLevel) waveInfo = GetWaveInfo(wave);
		int count = waveInfo.EnemyCount;
		int minLvl = waveInfo.MinLevel;
		int maxLvl = waveInfo.MaxLevel;
		List<EnemyModel> enemies = new List<EnemyModel>();
		Random rnd = new Random();
		List<EnemyModel> dbTemplates = _zoneRepo.GetEnemiesByLevelRange(minLvl, maxLvl);
		if (dbTemplates.Count == 0)
		{
			return enemies;
		}
		for (int i = 0; i < count; i++)
		{
			EnemyModel template = dbTemplates[rnd.Next(dbTemplates.Count)];
			float waveMultiplier = 1f + (float)wave * 0.05f;
			EnemyModel enemy = EnemyFactory.ScaleEnemy(template, waveMultiplier);
			enemies.Add(enemy);
		}
		return enemies;
	}

	public int CalculateReward(int wave, bool isFirstTime)
	{
		int baseReward = wave * 500;
		if (!isFirstTime)
		{
			return baseReward / 10;
		}
		return baseReward;
	}

	public void CompleteWave(CharacterModel hero, int completedWave)
	{
		if (completedWave == hero.MaxSurvivalWave)
		{
			hero.MaxSurvivalWave++;
			_charRepo.UpdateMaxSurvivalWave(hero.CharacterID, hero.MaxSurvivalWave);
		}
	}
}
