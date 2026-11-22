using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// Everything the game knows about the server's data, in one file:
// - the WIRE MODELS: the server's JSON, exactly as it arrives (moved here from SocketIOManager.cs);
// - the CLIENT MODELS: what the game and the views use;
// - GameDataConverter: the only code that turns one into the other.
// A change in what the server sends is handled here and nowhere else.

// ════════════════════════════════════════════════════════════════════════════
// Wire models — the server's JSON. Only SocketIOManager and GameDataConverter use these.
// ════════════════════════════════════════════════════════════════════════════

// ─── Emit Models ────────────────────────────────────────────────────────────
[Serializable]
public class MessageData
{
  public string type;
  public Data payload = new();
}

[Serializable]
public class Data
{
  public int betIndex;
  public string Event;
  public List<int> index;
  public int option;
}

// ─── SL-PT Server Response Models ───────────────────────────────────────────

// Top-level wrapper — handles initData, ResultData, WheelBonusResult, PickJackpotResult
[Serializable]
public class ServerResponse
{
  public string id { get; set; }
  public bool success { get; set; }
  // initData fields
  public GameData gameData { get; set; }
  public Features features { get; set; }
  public UiData uiData { get; set; }
  // ResultData fields
  public List<List<string>> matrix { get; set; }
  public SpinPayload payload { get; set; }
  // Shared
  public Player player { get; set; }
}

[Serializable]
public class GameData
{
  public List<double> bets { get; set; }
}

[Serializable]
public class Features
{
  public PinataMeters pinataMeters { get; set; }
}

[Serializable]
public class PinataMeters
{
  public bool enabled { get; set; }
  public MeterCurrentState currentState { get; set; }
}

[Serializable]
public class MeterCurrentState
{
  [JsonConverter(typeof(BoolOrIntConverter))] public int greenMeter { get; set; }
  [JsonConverter(typeof(BoolOrIntConverter))] public int redMeter { get; set; }
  [JsonConverter(typeof(BoolOrIntConverter))] public int blueMeter { get; set; }
  public int greenThreshold { get; set; }
  public int redThreshold { get; set; }
  public int blueThreshold { get; set; }
}

[Serializable]
public class SpinPayload
{
  public double winAmount { get; set; }
  public List<WaysWin> waysWins { get; set; }
  public List<CoinValue> coinWins { get; set; }
  public MetersUpdate meters { get; set; }
  public List<TriggeredFeature> triggeredFeatures { get; set; }
  public List<PendingFeature> pendingFeatures { get; set; }
  public bool isFreeSpinActive { get; set; }
  public int freeSpinsRemaining { get; set; }
  public bool isRedPinataFreeSpin { get; set; }
  public bool isBluePinataLinkBonus { get; set; }
  public bool isBluePinataFreeSpin { get; set; }
  public List<LinkBonusZone> linkBonusTargetZones { get; set; }
  public List<LockedCell> linkBonusLockedCells { get; set; }
}

[Serializable]
public class WaysWin
{
  public int symbolId { get; set; }
  public string symbolName { get; set; }
  public int matchCount { get; set; }
  public List<List<int>> positions { get; set; }
  public double payout { get; set; }
  public double basePayout { get; set; }
  public double jackpotPayout { get; set; }
}

[Serializable]
public class CoinValue
{
  public List<int> position { get; set; }
  public double value { get; set; }
}

[Serializable]
public class MetersUpdate
{
  [JsonConverter(typeof(BoolOrIntConverter))] public int greenMeter { get; set; }
  [JsonConverter(typeof(BoolOrIntConverter))] public int redMeter { get; set; }
  [JsonConverter(typeof(BoolOrIntConverter))] public int blueMeter { get; set; }
}

[Serializable]
public class TriggeredFeature
{
  public string feature { get; set; }
  public bool triggered { get; set; }
  public double awardValue { get; set; }
  // wheelBonus
  public string jackpotTier { get; set; }
  public List<string> spinHistory { get; set; }
  // pickJackpot
  public string goalJackpot { get; set; }
  // linkBonus
  public List<LockedCell> lockedCells { get; set; }
  public int respinsRemaining { get; set; }
  public bool grandTriggered { get; set; }
  public double grandBonus { get; set; }
  public double jackpotBonus { get; set; }
  public double baseWin { get; set; }
}

[Serializable]
public class PendingFeature
{
  public string feature { get; set; }
  public bool triggered { get; set; }
  public double bet { get; set; }
}

[Serializable]
public class LinkBonusZone
{
  public List<int> position { get; set; }
  public int zoneMultiplier { get; set; }
}

[Serializable]
public class LockedCell
{
  public List<int> position { get; set; }
  public int zoneMultiplier { get; set; }
  public string symbolId { get; set; }
  public double? prizeValue { get; set; }
}

// ─── Shared Models ───────────────────────────────────────────────────────────
[Serializable]
public class UiData
{
  public Paylines paylines { get; set; }
}

[Serializable]
public class Paylines
{
  public List<Symbol> symbols { get; set; }
}

[Serializable]
public class Symbol
{
  public int id { get; set; }
  public string name { get; set; }
  public List<double?> multiplier { get; set; }
  public string description { get; set; }
}

[Serializable]
public class Player
{
  public double balance { get; set; }
}

[Serializable]
public class BalanceSyncPayload
{
  public double balance;
}

[Serializable]
public class AuthTokenData
{
  public string cookie;
  public string socketURL;
  public string nameSpace;
}

public class BoolOrIntConverter : JsonConverter<int>
{
  public override int ReadJson(JsonReader reader, Type objectType, int existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    if (reader.TokenType == JsonToken.Boolean)
      return (bool)reader.Value ? 1 : 0;
    return Convert.ToInt32(reader.Value);
  }

  public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer)
  {
    writer.WriteValue(value);
  }
}

// ════════════════════════════════════════════════════════════════════════════
// Client models — what the game and the views use. Clean types: the matrix as ints, positions as
// Row / Col, ids as ints. Built only by GameDataConverter below; nothing outside this file and
// SocketIOManager sees a wire model.
// ════════════════════════════════════════════════════════════════════════════

internal class MeterValues
{
  internal int Green;
  internal int Red;
  internal int Blue;

  internal MeterValues(int green, int red, int blue)
  {
    Green = green;
    Red = red;
    Blue = blue;
  }
}

// From game:init.
internal class GameConfig
{
  internal List<double> Bets = new List<double>();
  // Mini, Minor, Major, Mega, Grand: multiplier[0] of symbol ids 3–7, 0 when the server sends none.
  internal double[] JackpotMultipliers = new double[5];
  // Null when the init carries no pinata-meter state.
  internal MeterValues MeterThresholds;
  internal MeterValues InitialMeters;
  internal double InitialBalance;
}

internal class CoinWin
{
  internal int Row;
  internal int Col;
  internal double Value;
}

internal class LinkZone
{
  internal int Row;
  internal int Col;
  internal int Multiplier;
}

internal class LinkCell
{
  internal int Row;
  internal int Col;
  internal int Multiplier;
  // Parsed once here. A missing or unreadable id is 2 (blue), the Link Bonus's own fallback.
  internal int SymbolId;
  internal double? Prize;
}

internal class JackpotWin
{
  internal string Tier;
  internal double Amount;
}

internal class WheelBonusResult
{
  internal string Tier;
  internal double Award;
  internal List<string> SpinHistory;

  // What the game shows when the response carries no wheel result at all.
  internal static WheelBonusResult Fallback() => new WheelBonusResult
  {
    Tier = "mini",
    Award = 0,
    SpinHistory = new List<string> { "mini" },
  };
}

internal class PickJackpotResult
{
  internal string GoalJackpot;
  internal double Award;
}

internal class LinkBonusResult
{
  internal double Award;
  internal List<LinkCell> LockedCells;
}

// One server response: a spin, a free spin, a Link Bonus round, a WHEELBONUS or a PICKJACKPOT reply.
// They all arrive in the same shape, so they share one type (as Wild Jokers' SpinResult); each
// feature's part is null unless the response carries it.
internal class RoundResult
{
  internal bool Success;      // carried for step 4 (failure handling); nothing reads it yet
  internal bool HasPayload;
  internal int[,] Matrix;     // [3,5], row-major, row 0 at the top. Null when absent or malformed.
  internal double WinAmount;
  internal double Balance;
  internal List<CoinWin> Coins;          // null when the response has none
  internal MeterValues Meters;           // null when the response has none
  internal JackpotWin BaseJackpot;       // the first jackpot (id 3–7) among the wins, or null
  internal List<string> TriggeredFeatures = new List<string>();  // in the server's order
  internal bool IsFreeSpinActive;        // Pick Jackpot's free-spin round goes on
  internal int FreeSpinsRemaining;
  internal bool IsLinkBonusActive;       // the Link Bonus goes on
  internal List<LinkZone> LinkTargetZones;  // null when absent
  internal List<LinkCell> LinkLockedCells;  // null when absent
  internal WheelBonusResult Wheel;       // null unless triggeredFeatures has "wheelBonus"
  internal PickJackpotResult Pick;       // null unless triggeredFeatures has "pickJackpot"
  internal LinkBonusResult Link;         // null unless triggeredFeatures has "linkBonus"
}

// ════════════════════════════════════════════════════════════════════════════
// The converter — the only code that knows the server's format.
// ════════════════════════════════════════════════════════════════════════════

internal static class GameDataConverter
{
  private const int RowCount = 3;
  private const int ReelCount = 5;
  private const int BlueSymbolId = 2;

  internal static GameConfig ToConfig(ServerResponse init)
  {
    var config = new GameConfig();
    if (init?.gameData?.bets != null) config.Bets = init.gameData.bets;

    var symbols = init?.uiData?.paylines?.symbols;
    if (symbols != null)
    {
      foreach (var symbol in symbols)
      {
        if (symbol.id >= 3 && symbol.id <= 7 && symbol.multiplier?[0] != null)
          config.JackpotMultipliers[symbol.id - 3] = symbol.multiplier[0].Value;
      }
    }

    var meters = init?.features?.pinataMeters?.currentState;
    if (meters != null)
    {
      config.MeterThresholds = new MeterValues(meters.greenThreshold, meters.redThreshold, meters.blueThreshold);
      config.InitialMeters = new MeterValues(meters.greenMeter, meters.redMeter, meters.blueMeter);
    }

    config.InitialBalance = init?.player?.balance ?? 0;
    return config;
  }

  // fallbackBalance is used only if a response arrives without a player block (the last known balance).
  internal static RoundResult ToResult(ServerResponse response, double fallbackBalance)
  {
    var payload = response?.payload;
    var result = new RoundResult
    {
      Success = response?.success ?? false,
      HasPayload = payload != null,
      Matrix = ToMatrix(response?.matrix),
      WinAmount = payload?.winAmount ?? 0,
      Balance = response?.player?.balance ?? fallbackBalance,
      Coins = ToCoins(payload?.coinWins),
      Meters = payload?.meters != null
        ? new MeterValues(payload.meters.greenMeter, payload.meters.redMeter, payload.meters.blueMeter)
        : null,
      BaseJackpot = ToBaseJackpot(payload?.waysWins),
      IsFreeSpinActive = payload?.isFreeSpinActive ?? false,
      FreeSpinsRemaining = payload?.freeSpinsRemaining ?? 0,
      IsLinkBonusActive = payload?.isBluePinataLinkBonus ?? false,
      LinkTargetZones = ToZones(payload?.linkBonusTargetZones),
      LinkLockedCells = ToCells(payload?.linkBonusLockedCells),
    };

    if (payload?.pendingFeatures != null)
    {
      foreach (var pending in payload.pendingFeatures)
        if (pending != null && pending.triggered) result.TriggeredFeatures.Add(pending.feature);
    }

    var triggered = payload?.triggeredFeatures;
    var wheel = triggered?.Find(f => f.feature == "wheelBonus");
    if (wheel != null)
    {
      string tier = wheel.jackpotTier ?? "mini";
      result.Wheel = new WheelBonusResult
      {
        Tier = tier,
        Award = wheel.awardValue,
        SpinHistory = wheel.spinHistory ?? new List<string> { tier },
      };
    }

    var pick = triggered?.Find(f => f.feature == "pickJackpot");
    if (pick != null)
    {
      result.Pick = new PickJackpotResult { GoalJackpot = pick.goalJackpot, Award = pick.awardValue };
    }

    var link = triggered?.Find(f => f.feature == "linkBonus");
    if (link != null)
    {
      // The feature's own locked cells; the payload's when the feature sends none.
      result.Link = new LinkBonusResult
      {
        Award = link.awardValue,
        LockedCells = ToCells(link.lockedCells ?? payload?.linkBonusLockedCells),
      };
    }

    return result;
  }

  // The server sends the matrix row-major as strings. Anything that is not 3 x 5 numbers is logged
  // and left null, so the spin is abandoned cleanly instead of failing half-way through drawing it.
  private static int[,] ToMatrix(List<List<string>> matrix)
  {
    if (matrix == null) return null;
    if (matrix.Count < RowCount)
    {
      Debug.LogError($"[GameDataConverter] Matrix has {matrix.Count} rows, expected {RowCount}.");
      return null;
    }

    var result = new int[RowCount, ReelCount];
    for (int row = 0; row < RowCount; row++)
    {
      if (matrix[row] == null || matrix[row].Count < ReelCount)
      {
        Debug.LogError($"[GameDataConverter] Matrix row {row} is incomplete.");
        return null;
      }
      for (int col = 0; col < ReelCount; col++)
      {
        if (!int.TryParse(matrix[row][col], out result[row, col]))
        {
          Debug.LogError($"[GameDataConverter] Matrix cell [{row},{col}] is not a number: '{matrix[row][col]}'.");
          return null;
        }
      }
    }
    return result;
  }

  private static List<CoinWin> ToCoins(List<CoinValue> coins)
  {
    if (coins == null) return null;
    var result = new List<CoinWin>(coins.Count);
    foreach (var coin in coins)
    {
      if (coin?.position == null || coin.position.Count < 2) continue;
      result.Add(new CoinWin { Row = coin.position[0], Col = coin.position[1], Value = coin.value });
    }
    return result;
  }

  private static List<LinkZone> ToZones(List<LinkBonusZone> zones)
  {
    if (zones == null) return null;
    var result = new List<LinkZone>(zones.Count);
    foreach (var zone in zones)
    {
      if (zone?.position == null || zone.position.Count < 2) continue;
      result.Add(new LinkZone { Row = zone.position[0], Col = zone.position[1], Multiplier = zone.zoneMultiplier });
    }
    return result;
  }

  private static List<LinkCell> ToCells(List<LockedCell> cells)
  {
    if (cells == null) return null;
    var result = new List<LinkCell>(cells.Count);
    foreach (var cell in cells)
    {
      if (cell?.position == null || cell.position.Count < 2) continue;
      result.Add(new LinkCell
      {
        Row = cell.position[0],
        Col = cell.position[1],
        Multiplier = cell.zoneMultiplier,
        SymbolId = int.TryParse(cell.symbolId, out int id) ? id : BlueSymbolId,
        Prize = cell.prizeValue,
      });
    }
    return result;
  }

  // The base game's jackpot win: the first win on a jackpot symbol (ids 3–7). A second jackpot on the
  // same spin is not shown separately (GameExplanation §8).
  private static JackpotWin ToBaseJackpot(List<WaysWin> wins)
  {
    var win = wins?.Find(w => w.symbolId >= 3 && w.symbolId <= 7);
    return win == null ? null : new JackpotWin { Tier = win.symbolName, Amount = win.jackpotPayout };
  }
}
