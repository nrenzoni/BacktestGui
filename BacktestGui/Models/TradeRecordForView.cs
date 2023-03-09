using NodaTime;

namespace BacktestGui.Models;

public record TradeRecordForView(
    int Number,
    string Symbol,
    Instant OpenTime,
    Instant CloseTime,
    bool IsLongTrade,
    decimal AvgEntryPrice,
    decimal AvgExitPrice,
    int CumEntryShares,
    int CumExitShares,
    decimal OpenPrice,
    decimal ClosePrice,
    decimal MinPrice,
    decimal MaxPrice,
    decimal Pnl);