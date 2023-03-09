namespace BacktestGui.Configurations;

public class IndicatorGrpcConfig
{
    public string ConnectionString { get; set; }
    
    public uint? RetryCount { get; set; }
}