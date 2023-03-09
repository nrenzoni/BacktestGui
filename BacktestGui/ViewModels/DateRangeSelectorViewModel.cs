using System;
using System.Reactive.Linq;
using NodaTime;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BacktestGui.ViewModels;

public class DateRangeSelectorViewModel : ViewModelBase
{
    public DateRangeSelectorViewModel()
    {
        var startDateObservable = this.WhenAnyValue(x => x.SelectedStartDate)
            .WhereNotNull()
            .Select(x => x > SelectedEndDate ? SelectedEndDate.Value : x.Value);
        var connectableStartDateObservable = startDateObservable.Publish();
        connectableStartDateObservable
            .BindTo(this, x => x.SelectedStartDate);
        connectableStartDateObservable
            .Select(x => LocalDate.FromDateTime(x))
            .BindTo(this, x => x.SelectedStartLocalDate);
        connectableStartDateObservable.Connect();

        var endDateObservable = this.WhenAnyValue(x => x.SelectedEndDate)
            .WhereNotNull()
            .Select(x => x < SelectedStartDate ? SelectedStartDate.Value : x.Value);
        var connectableEndDateObservable = endDateObservable.Publish();
        connectableEndDateObservable
            .BindTo(this, x => x.SelectedEndDate);
        connectableEndDateObservable
            .Select(x => LocalDate.FromDateTime(x))
            .BindTo(this, x => x.SelectedEndLocalDate);
        connectableEndDateObservable.Connect();
    }

    [Reactive] public DateTime? SelectedStartDate { get; set; } = new(2022, 1, 3);

    [Reactive] public LocalDate? SelectedStartLocalDate { get; set; }

    [Reactive] public DateTime? SelectedEndDate { get; set; } = new(2022, 1, 15);

    [Reactive] public LocalDate? SelectedEndLocalDate { get; set; }
}