using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUpgradable
{
	public List<UpgradeData> GetUpgradeOptions();

	public CharacteristicsCollection Characteristics { get; }
}
