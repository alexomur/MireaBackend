using System;

namespace Sorter.Features;

public static class MergeSorter
{
    public static int[] Sort(int[]? source)
    {
        if (source == null) return Array.Empty<int>();
        int n = source.Length;
        if (n <= 1) return source.AsSpan().ToArray();
        int[] buffer = new int[n];
        int[] a = source.AsSpan().ToArray();
        int[] b = buffer;
        int width = 1;
        while (width < n)
        {
            int i = 0;
            while (i < n)
            {
                int left = i;
                int mid = Math.Min(i + width, n);
                int right = Math.Min(i + 2 * width, n);
                int p = left;
                int q = mid;
                int k = left;
                while (p < mid && q < right)
                {
                    if (a[p] <= a[q]) b[k++] = a[p++];
                    else b[k++] = a[q++];
                }
                while (p < mid) b[k++] = a[p++];
                while (q < right) b[k++] = a[q++];
                i += 2 * width;
            }
            (a, b) = (b, a);
            width *= 2;
        }
        return a;
    }
}