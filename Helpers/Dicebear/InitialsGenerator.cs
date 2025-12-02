namespace OneDesk.Helpers.Dicebear;

public static class InitialsGenerator
{
    // 默认颜色列表 (来自 @dicebear/initials/src/schema.ts)
    private static readonly string[] _defaultColors =
    [
        "e53935", "d81b60", "8e24aa", "5e35b1",
        "3949ab", "1e88e5", "039be5", "00acc1",
        "00897b", "43a047", "7cb342", "c0ca33",
        "fdd835", "ffb300", "fb8c00", "f4511e"
    ];

    public static string GenerateSvg(string seed)
    {
        var color = GetBackgroundColor(seed);
        var upper = seed.ToUpper();
        var initials = upper[..1];
        return BuildSvg(color, initials);
    }

    private static string BuildSvg(string color, string initials)
    {
        return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 100\">" +
               "<mask id=\"viewboxMask\"><rect width=\"100\" height=\"100\" rx=\"0\" ry=\"0\" x=\"0\" y=\"0\" fill=\"#fff\" />" +
               $"</mask><g mask=\"url(#viewboxMask)\"><rect fill=\"{color}\" width=\"100\" height=\"100\" x=\"0\" y=\"0\" />" +
               $"<text x=\"50%\" y=\"50%\" font-family=\"Arial, sans-serif\" font-size=\"50\" font-weight=\"400\" fill=\"#ffffff\" text-anchor=\"middle\" dy=\"17.800\">{initials}</text></g></svg>";
    }

    /// <summary>
    /// 根据 Seed 获取背景色 (模拟 DiceBear Initials 逻辑)
    /// </summary>
    /// <param name="seed">用户输入的 Seed 字符串</param>
    /// <returns>Hex 颜色代码 (例如 #e53935)</returns>
    public static string GetBackgroundColor(string seed)
    {
        // 1. 初始化主 PRNG
        var prng = new Prng(seed);
        prng.Next();
        prng.Next(); // 在 DiceBear Initials 获取背景色前会调用两次 Next()，用途不详（懒得查了）

        // 2. 对颜色列表进行洗牌
        var shuffledBackgroundColors = prng.Shuffle(_defaultColors);
        if (shuffledBackgroundColors.Count <= 1)
        {
            shuffledBackgroundColors = _defaultColors.ToList();
            prng.Next();
        }
        else
        {
            shuffledBackgroundColors = prng.Shuffle(_defaultColors);
        }
        if (shuffledBackgroundColors.Count == 0)
        {
            shuffledBackgroundColors = ["transparent"];
        }

        // 3. 取洗牌后的第一个颜色
        var color = shuffledBackgroundColors[0];

        // 4. 格式化输出
        return color == "transparent" ? color : $"#{color}";
    }

}