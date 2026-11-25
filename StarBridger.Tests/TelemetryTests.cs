using Microsoft.Extensions.Logging.Abstractions;
using Sc.Game.Telemetry.V1;
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.Enums;
using StarBridger.Contracts.Telemetry.Game.v1;
using StarBridger.Core.Services;
using StarBridger.Telemetry;
using StarBridger.Telemetry.Utils;
using Game = Sc.Game.Telemetry.V1;

namespace StarBridger.Tests;

public sealed class TelemetryTests
{
  [Fact]
  public void VehicleMapperPreservesFieldsAndNormalizesUnavailableAltitude()
  {
    Game.VehicleState source = new()
    {
      Name = "Test ship",
      ClassName = "Test class",
      Manufacturer = "Test manufacturer",
      Health = 0.75f,
      LightsEnabled = true,
      Flight = new Game.VehicleFlight
      {
        Instruments = new Game.VehicleFlightInstruments
        {
          Speed = 120.5f,
          Altitude = float.PositiveInfinity,
          RadarAltitude = float.MaxValue
        }
      }
    };

    VehicleTelemetry mapped = StarCitizenTelemetryMapper.MapVehicle(source);

    Assert.Equal("Test ship", mapped.Name);
    Assert.Equal("Test class", mapped.ClassName);
    Assert.Equal("Test manufacturer", mapped.Manufacturer);
    Assert.Equal(0.75f, mapped.Health);
    Assert.True(mapped.LightsEnabled);
    Assert.Equal(120.5f, mapped.Flight!.Instruments!.Speed);
    Assert.Null(mapped.Flight.Instruments.Altitude);
    Assert.Null(mapped.Flight.Instruments.RadarAltitude);
    Assert.Null(mapped.Doors);
  }

  [Fact]
  public void VehicleMapperKeepsFiniteAltitude()
  {
    VehicleTelemetry mapped = StarCitizenTelemetryMapper.MapVehicle(new Game.VehicleState
    {
      Flight = new Game.VehicleFlight
      {
        Instruments = new Game.VehicleFlightInstruments { Altitude = 123.25f, RadarAltitude = 12.5f }
      }
    });

    Assert.Equal(123.25f, mapped.Flight!.Instruments!.Altitude);
    Assert.Equal(12.5f, mapped.Flight.Instruments.RadarAltitude);
  }

  [Fact]
  public void ActorMapperMapsVitalsStateAndEnvironment()
  {
    ActorTelemetry mapped = StarCitizenTelemetryMapper.MapActor(new Game.ActorFrame
    {
      Vitals = new Game.ActorVitals
      {
        Health = new Game.Gauge { Current = 75, Max = 100 },
        HeartRate = 88,
        IntoxicationLevel = 2
      },
      State = new Game.ActorState { SeatName = "Pilot", UecBalance = 9000 },
      Environment = new Game.ActorEnvironment { BreathableAtmosphere = true }
    });

    Assert.Equal(75, mapped.Vitals!.Health!.Current);
    Assert.Equal(100, mapped.Vitals.Health.Max);
    Assert.Equal(88, mapped.Vitals.HeartRate);
    Assert.Equal(2, mapped.Vitals.IntoxicationLevel);
    Assert.Equal("Pilot", mapped.State!.SeatName);
    Assert.Equal(9000, mapped.State.UecBalance);
    Assert.True(mapped.Environment!.BreathableAtmosphere);
  }

  [Fact]
  public void WeaponsMapperMapsSettingsWeaponsAndCountermeasures()
  {
    WeaponsTelemetry mapped = StarCitizenTelemetryMapper.MapWeapons(new Game.WeaponsFrame
    {
      Settings = new Game.WeaponsSettings { GimbalsLocked = true, StaggeredFire = true },
      PresetIndex = 3,
      PresetName = "Default",
      Weapons = { new Game.WeaponState { Name = "Laser", IsPowered = true, ReadyToFire = true, ContainedInPreset = true } },
      Countermeasures = { new Game.Countermeasures { Amount = 4, Type = Game.Countermeasures.Types.CountermeasureType.Decoy } }
    });

    Assert.True(mapped.Settings!.GimbalsLocked);
    Assert.True(mapped.Settings.StaggeredFire);
    Assert.Equal(3u, mapped.PresetIndex);
    Assert.Equal("Default", mapped.PresetName);
    Assert.Equal("Laser", Assert.Single(mapped.Weapons).Name);
    Assert.True(mapped.Weapons[0].ReadyToFire);
    Assert.Equal(4u, Assert.Single(mapped.Countermeasures).Amount);
    Assert.Equal(CountermeasureType.Decoy, mapped.Countermeasures[0].Type);
  }

  [Fact]
  public void VehicleComparerIgnoresSmallSpeedNoiseButDetectsMaterialChanges()
  {
    Game.VehicleFrame first = VehicleFrameWithSpeed(0.005f, -0.004f);
    Game.VehicleFrame equivalent = VehicleFrameWithSpeed(-0.006f, 0.009f);
    Game.VehicleFrame changed = VehicleFrameWithSpeed(0.02f, 0);

    Assert.False(TelemetryComparer.HasVehicleChanged(first, equivalent));
    Assert.True(TelemetryComparer.HasVehicleChanged(first, changed));
    Assert.False(TelemetryComparer.HasChanged<Game.ActorFrame>(null, null));
    Assert.True(TelemetryComparer.HasChanged<Game.ActorFrame>(null, new Game.ActorFrame()));
  }

  [Fact]
  public async Task TelemetryServicePublishesInitialStatusAndRegistersShellCommands()
  {
    StarBridgerShell shell = new(NullLogger<StarBridgerShell>.Instance);
    StarCitizenTelemetryService service = new(NullLogger<StarCitizenTelemetryService>.Instance, shell);

    Assert.Equal(StarCitizenConnectionStatus.Disconnected, service.Connection.State.Status);
    Assert.Contains(shell.GetCommandDefinitions(), definition => definition.CommandText == "telemetry.status");
    Assert.Contains(shell.GetCommandDefinitions(), definition => definition.CommandText == "telemetry.extract");
    Assert.Contains(shell.GetCommandDefinitions(), definition => definition.CommandText == "telemetry.reconnect");

    List<string> output = [];
    Assert.True(await shell.ExecuteCommand(new Command { CommandText = "telemetry.status" }, output.Add, null, CancellationToken.None));
    Assert.Contains(output, line => line == "Status: Disconnected");
    Assert.Contains(output, line => line.Contains("127.0.0.1:2942", StringComparison.Ordinal));

    Assert.False(await shell.ExecuteCommand(new Command { CommandText = "telemetry.extract" }, output.Add, null, CancellationToken.None));
    Assert.Contains(output, line => line.Contains("No telemetry data available", StringComparison.Ordinal));

    await service.StopAsync(CancellationToken.None);
    await shell.StopAsync(CancellationToken.None);
  }

  private static Game.VehicleFrame VehicleFrameWithSpeed(float speed, float verticalSpeed)
  {
    return new Game.VehicleFrame
    {
      State = new Game.VehicleState
      {
        Flight = new Game.VehicleFlight
        {
          Instruments = new Game.VehicleFlightInstruments
          {
            Speed = speed,
            VerticalSpeedIndicator = verticalSpeed
          }
        }
      }
    };
  }
}
