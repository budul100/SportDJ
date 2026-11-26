using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.GridModule.ViewModels;

/// <summary>
/// Remembers the view model of each model object (player, group, tab), so a changed grid only touches
/// what really changed. Players which keep their model keep their view model, their files are not read again.
/// </summary>
internal sealed class ViewModelCache
{
    #region Private Fields

    private readonly Dictionary<object, CellViewModel> entries = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<object> used = new(ReferenceEqualityComparer.Instance);

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Brings the items of the target into the order of the desired items. Items which are
    /// part of both lists stay in the collection, so their views are kept.
    /// </summary>
    public static void Synchronize<T>(ObservableCollection<T> target, IReadOnlyList<T> desired)
        where T : class
    {
        foreach (var item in target.ToArray())
        {
            if (!desired.Contains(item))
            {
                target.Remove(item);
            }
        }

        for (var index = 0; index < desired.Count; index++)
        {
            var item = desired[index];

            if (index < target.Count
                && ReferenceEquals(target[index], item))
            {
                continue;
            }

            var current = target.IndexOf(item);

            if (current >= 0)
            {
                target.Move(
                    oldIndex: current,
                    newIndex: index);
            }
            else
            {
                target.Insert(
                    index: index,
                    item: item);
            }
        }
    }

    public void Add(object model, CellViewModel viewModel)
    {
        entries[model] = viewModel;

        used.Add(model);
    }

    public void BeginUpdate()
    {
        used.Clear();
    }

    public void DisposeAll()
    {
        var all = entries.Values.ToArray();

        entries.Clear();
        used.Clear();

        foreach (var viewModel in all)
        {
            viewModel.Dispose();
        }
    }

    public T Get<T>(object model)
        where T : CellViewModel
    {
        return entries.TryGetValue(
            key: model,
            value: out var viewModel)
            ? viewModel as T
            : default;
    }

    /// <summary>
    /// Disposes the view models of all models which were not used since BeginUpdate.
    /// </summary>
    public void Sweep()
    {
        var unused = entries
            .Where(e => !used.Contains(e.Key)).ToArray();

        foreach (var entry in unused)
        {
            entries.Remove(entry.Key);

            entry.Value.Dispose();
        }
    }

    public void Use(object model)
    {
        used.Add(model);
    }

    #endregion Public Methods
}
