using System.Collections.Generic;
using GoogleSheet.Type;

namespace GoogleSheet.Type
{
    // 시트 컬럼 타입: statreqs
    // 셀 포맷 예시: (Intelligence:50,Charm:30)
    [Type(Type: typeof(List<StatRequirement>), TypeName: new string[] { "statreqs", "StatReqs" })]
    public class StatRequirementListType : IType
    {
        public object DefaultValue => new List<StatRequirement>();

        public object Read(string value)
        {
            var result = new List<StatRequirement>();
            if (string.IsNullOrEmpty(value) || value == "()")
                return result;

            var entries = ReadUtil.GetBracketValueToArray(value);
            if (entries == null) return result;

            foreach (var entry in entries)
            {
                var parts = entry.Split(':');
                if (parts.Length != 2) continue;

                if (System.Enum.TryParse<StatType>(parts[0].Trim(), out var statType))
                {
                    result.Add(new StatRequirement
                    {
                        statType = statType,
                        minValue = int.Parse(parts[1].Trim())
                    });
                }
            }
            return result;
        }

        public string Write(object value)
        {
            var list = (List<StatRequirement>)value;
            if (list == null || list.Count == 0) return "()";

            var parts = new List<string>();
            foreach (var req in list)
                parts.Add($"{req.statType}:{req.minValue}");

            return "(" + string.Join(",", parts) + ")";
        }
    }
}
