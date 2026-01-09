using System.Collections.Generic;
using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;

namespace rpg_deneme.Business;

public class ZoneManager
{
	private ZoneRepository _repo = new ZoneRepository();

	private CharacterRepository _charRepo = new CharacterRepository();

	public List<ZoneModel> GetAvailableZones(CharacterModel hero)
	{
		if (hero.MaxUnlockedZoneID < 1)
		{
			hero.MaxUnlockedZoneID = 1;
		}
		List<ZoneModel> zones = _repo.GetZones();
		foreach (ZoneModel z in zones)
		{
			z.IsUnlocked = z.ZoneID <= hero.MaxUnlockedZoneID;
		}
		return zones;
	}

	public EnemyModel GetEnemyForZone(int zoneId, bool spawnBoss)
	{
		if (spawnBoss)
		{
			return _repo.GetBossForZone(zoneId);
		}
		return _repo.GetRandomEnemy(zoneId);
	}

	public bool CanEnterDifficulty(int charId, int zoneId, Enums.ZoneDifficulty diff)
	{
		if (diff == Enums.ZoneDifficulty.Easy)
		{
			return true;
		}
		ZoneProgressDTO data = _repo.GetZoneProgressData(charId, zoneId);
		return diff switch
		{
			Enums.ZoneDifficulty.Normal => _repo.GetBossKilled(charId, zoneId, Enums.ZoneDifficulty.Easy) || data.ProgressEasy >= 100 || data.ProgressEasy == 99, 
			Enums.ZoneDifficulty.Hard => _repo.GetBossKilled(charId, zoneId, Enums.ZoneDifficulty.Normal) || data.ProgressNormal >= 100 || data.ProgressNormal == 99, 
			_ => false, 
		};
	}

	public int GetProgressValue(int charId, int zoneId, Enums.ZoneDifficulty diff)
	{
		ZoneProgressDTO data = _repo.GetZoneProgressData(charId, zoneId);
		if (1 == 0)
		{
		}
		int num = diff switch
		{
			Enums.ZoneDifficulty.Easy => data.ProgressEasy, 
			Enums.ZoneDifficulty.Normal => data.ProgressNormal, 
			Enums.ZoneDifficulty.Hard => data.ProgressHard, 
			_ => 0, 
		};
		if (1 == 0)
		{
		}
		int val = num;
		if (_repo.GetBossKilled(charId, zoneId, diff))
		{
			return 100;
		}
		return val;
	}

	public bool IsDifficultyCompleted(int charId, int zoneId, Enums.ZoneDifficulty diff)
	{
		return _repo.GetBossKilled(charId, zoneId, diff);
	}

	public void AddProgress(int charId, int zoneId, Enums.ZoneDifficulty diff, int amount)
	{
		if (IsDifficultyCompleted(charId, zoneId, diff))
		{
			_repo.SaveZoneProgress(charId, zoneId, diff, 100);
			return;
		}
		ZoneProgressDTO d = _repo.GetZoneProgressData(charId, zoneId);
		if (1 == 0)
		{
		}
		int num;
		if (diff == Enums.ZoneDifficulty.Easy)
		{
			num = d.ProgressEasy;
		}
		else
		{
			ZoneProgressDTO d2 = d;
			if (diff == Enums.ZoneDifficulty.Normal)
			{
				num = d2.ProgressNormal;
			}
			else
			{
				ZoneProgressDTO d3 = d;
				num = ((diff == Enums.ZoneDifficulty.Hard) ? d3.ProgressHard : 0);
			}
		}
		if (1 == 0)
		{
		}
		int current = num;
		if (current < 100)
		{
			_repo.SaveZoneProgress(charId, zoneId, diff, current + amount);
		}
	}

	public void ResetProgress(int charId, int zoneId, Enums.ZoneDifficulty diff)
	{
		if (IsDifficultyCompleted(charId, zoneId, diff))
		{
			_repo.SaveZoneProgress(charId, zoneId, diff, 100);
		}
		else
		{
			_repo.SaveZoneProgress(charId, zoneId, diff, 0);
		}
	}

	public void SetProgress(int charId, int zoneId, Enums.ZoneDifficulty diff, int value)
	{
		if (value < 0)
		{
			value = 0;
		}
		if (value > 100)
		{
			value = 100;
		}
		if (IsDifficultyCompleted(charId, zoneId, diff))
		{
			value = 100;
		}
		_repo.SaveZoneProgress(charId, zoneId, diff, value);
	}

	public bool IsBossKilled(int charId, int zoneId, Enums.ZoneDifficulty diff)
	{
		return _repo.GetBossKilled(charId, zoneId, diff);
	}

	public void MarkBossKilled(int charId, int zoneId, Enums.ZoneDifficulty diff)
	{
		_repo.SetBossKilled(charId, zoneId, diff, killed: true);
		_repo.SaveZoneProgress(charId, zoneId, diff, 100);
	}
}
