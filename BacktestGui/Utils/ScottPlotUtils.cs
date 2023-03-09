using ScottPlot.Renderable;

namespace BacktestGui.Utils;

public static class ScottPlotUtils
{
    public static void SetAxisLimits(AxisDimensions axis, double min, double max)
    {
        axis.SetBoundsInner(min, max);
        axis.SetBoundsOuter(min, max);
    }
}