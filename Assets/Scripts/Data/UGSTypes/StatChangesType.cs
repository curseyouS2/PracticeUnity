using System;
using GoogleSheet.Type;

namespace GoogleSheet.Type
{
    // 시트 컬럼 타입: statchanges
    // 셀 포맷 예시: (intelligence:5,money:-100,fatigue:10)
    // 0인 필드는 생략 가능
    [Type(Type: typeof(StatChanges), TypeName: new string[] { "statchanges", "StatChanges" })]
    public class StatChangesType : IType
    {
        public object DefaultValue => new StatChanges();

        public object Read(string value)
        {
            var result = new StatChanges();
            if (string.IsNullOrEmpty(value) || value == "()")
                return result;

            var entries = ReadUtil.GetParenthesisValueToArray(value);
            if (entries == null)
                entries = value.Split(','); // 괄호 없이 key:value 형태인 경우

            foreach (var entry in entries)
            {
                var parts = entry.Split(':');
                if (parts.Length != 2) continue;
                string key = parts[0].Trim().ToLower();
                int val = int.Parse(parts[1].Trim());

                switch (key)
                {
                    case "intelligence": result.intelligence = val; break;
                    case "charm":        result.charm        = val; break;
                    case "courage":      result.courage      = val; break;
                    case "money":        result.money        = val; break;
                    case "fatigue":      result.fatigue      = val; break;
                    case "moral":        result.moral        = val; break;
                    case "physical":     result.physical     = val; break;
                    case "mental":       result.mental       = val; break;
                }
            }
            return result;
        }

        public string Write(object value)
        {
            var s = (StatChanges)value;
            var parts = new System.Collections.Generic.List<string>();

            if (s.intelligence != 0) parts.Add($"intelligence:{s.intelligence}");
            if (s.charm        != 0) parts.Add($"charm:{s.charm}");
            if (s.courage      != 0) parts.Add($"courage:{s.courage}");
            if (s.money        != 0) parts.Add($"money:{s.money}");
            if (s.fatigue      != 0) parts.Add($"fatigue:{s.fatigue}");
            if (s.moral        != 0) parts.Add($"moral:{s.moral}");
            if (s.physical     != 0) parts.Add($"physical:{s.physical}");
            if (s.mental       != 0) parts.Add($"mental:{s.mental}");

            return "(" + string.Join(",", parts) + ")";
        }
    }
}
