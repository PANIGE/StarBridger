using StarBridger.Contracts.Telemetry.Game;
using StarBridger.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Telemetry.Versionning;

public class ContractDowngrader(Version targetVersion)
{
  private readonly Version _targetVersion = targetVersion.Normalize();

  public static VersionnableContract Downgrade(object contract, Version targetVersion)
  {
    if (contract is not VersionnableContract versionnableContract)
    {
      throw new ArgumentException($"Contract of type {contract.GetType().Name} is not a VersionnableContract.");
    }

    ContractDowngrader downgrader = new(targetVersion);
    return downgrader.DowngradeContract(versionnableContract);
  }

  public VersionnableContract DowngradeContract(VersionnableContract contract, VersionnableContract? original = null)
  {
    if (contract.CurrentVersion <= _targetVersion)
    {
      return contract;
    }

    if (contract.PreviousVersion is null)
    {
      throw new InvalidOperationException($"Cannot downgrade {contract.GetType().Name} to version {_targetVersion} as version {contract.CurrentVersion} cannot be downgraded further (from {original?.CurrentVersion ?? contract.CurrentVersion})");
    }

    VersionnableContract downgradedContract = DowngradeStep(contract);

    if (downgradedContract.CurrentVersion >= contract.CurrentVersion)
    {
      throw new InvalidOperationException($"Invalid downgrade from {contract.CurrentVersion} to {downgradedContract.CurrentVersion}.");
    }

    return DowngradeContract(downgradedContract, original ?? contract);
  }

  private VersionnableContract DowngradeStep(VersionnableContract contract)
  {
    throw new NotImplementedException();
  }
}