using CollageGenerator.Models;

namespace CollageGenerator.Services;

public sealed class CollageLayoutService
{
    public CollageLayout Generate(IReadOnlyList<ImageItem> source, LayoutOptions options)
    {
        if (source.Count == 0) return new CollageLayout(options.Width, options.Height, Array.Empty<LayoutRect>(), 1);

        var images = Order(source, options).ToList();
        var maxRows = Math.Min(images.Count, Math.Max(1, (int)Math.Ceiling(Math.Sqrt(images.Count) * 3)));
        var best = BuildRows(images, options, 1);

        for (var rows = 2; rows <= maxRows; rows++)
        {
            var candidate = BuildRows(images, options, rows);
            if (candidate.EmptyAreaRatio < best.EmptyAreaRatio) best = candidate;
        }

        return best;
    }

    private static IEnumerable<ImageItem> Order(IReadOnlyList<ImageItem> images, LayoutOptions options)
    {
        var list = images.ToList();
        if (options.OrderMode == ImageOrderMode.Automatic) return list.OrderByDescending(i => i.AspectRatio);
        if (options.OrderMode == ImageOrderMode.Random) return list.OrderBy(_ => Random.Shared.Next());
        return list;
    }

    private static CollageLayout BuildRows(IReadOnlyList<ImageItem> images, LayoutOptions options, int targetRows)
    {
        var rows = PartitionRows(images, options, targetRows);
        var usableWidth = Math.Max(1, options.Width - 2 * options.Margin);
        var usableHeight = Math.Max(1, options.Height - 2 * options.Margin);
        var rowHeights = rows
            .Select(r => Math.Max(1, usableWidth - options.Spacing * Math.Max(0, r.Count - 1)) / Math.Max(0.0001, r.Sum(i => i.AspectRatio)))
            .ToList();

        var totalHeight = rowHeights.Sum() + options.Spacing * Math.Max(0, rows.Count - 1);
        var fitScale = Math.Min(1, usableHeight / Math.Max(1, totalHeight));
        var fittedHeight = totalHeight * fitScale;
        var y = options.Margin + Math.Max(0, (usableHeight - fittedHeight) / 2);
        var rects = new List<LayoutRect>();
        var usedArea = 0d;

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var h = rowHeights[r] * fitScale;
            var rowWidth = row.Sum(i => i.AspectRatio * h) + options.Spacing * fitScale * Math.Max(0, row.Count - 1);
            var x = options.Margin + Math.Max(0, (usableWidth - rowWidth) / 2);

            foreach (var image in row)
            {
                var w = image.AspectRatio * h;
                rects.Add(new LayoutRect(image, x, y, w, h));
                usedArea += w * h;
                x += w + options.Spacing * fitScale;
            }

            y += h + options.Spacing * fitScale;
        }

        var empty = Math.Clamp(1 - usedArea / Math.Max(1, usableWidth * usableHeight), 0, 1);
        return new CollageLayout(options.Width, options.Height, rects, empty);
    }

    private static List<List<ImageItem>> PartitionRows(IReadOnlyList<ImageItem> images, LayoutOptions options, int targetRows)
    {
        var count = images.Count;
        var rows = Math.Min(targetRows, count);
        var usableWidth = Math.Max(1, options.Width - 2 * options.Margin);
        var usableHeight = Math.Max(1, options.Height - 2 * options.Margin);
        var idealRowHeight = Math.Max(1, usableHeight - options.Spacing * Math.Max(0, rows - 1)) / rows;
        var cost = new double[rows + 1, count + 1];
        var split = new int[rows + 1, count + 1];

        for (var r = 0; r <= rows; r++)
        for (var i = 0; i <= count; i++)
            cost[r, i] = double.PositiveInfinity;
        cost[0, 0] = 0;

        for (var r = 1; r <= rows; r++)
        {
            for (var i = r; i <= count; i++)
            {
                var aspectSum = 0d;
                for (var j = i - 1; j >= r - 1; j--)
                {
                    aspectSum += images[j].AspectRatio;
                    var imageCount = i - j;
                    var rowWidth = Math.Max(1, usableWidth - options.Spacing * Math.Max(0, imageCount - 1));
                    var rowHeight = rowWidth / Math.Max(0.0001, aspectSum);
                    var rowCost = Math.Pow(rowHeight - idealRowHeight, 2) + imageCount * 0.01;
                    var candidate = cost[r - 1, j] + rowCost;
                    if (candidate >= cost[r, i]) continue;
                    cost[r, i] = candidate;
                    split[r, i] = j;
                }
            }
        }

        var result = new List<List<ImageItem>>();
        var end = count;
        for (var r = rows; r >= 1; r--)
        {
            var start = split[r, end];
            result.Add(images.Skip(start).Take(end - start).ToList());
            end = start;
        }

        result.Reverse();
        return result;
    }
}
