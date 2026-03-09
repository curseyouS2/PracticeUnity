using System.Collections.Generic;
using GoogleSheet.Type;

namespace GoogleSheet.Type
{
    // 시트 컬럼 타입: itemreqs
    // 셀 포맷 예시: (item_book:1,item_pen:2)
    [Type(Type: typeof(List<ItemRequirement>), TypeName: new string[] { "itemreqs", "ItemReqs" })]
    public class ItemRequirementListType : IType
    {
        public object DefaultValue => new List<ItemRequirement>();

        public object Read(string value)
        {
            var result = new List<ItemRequirement>();
            if (string.IsNullOrEmpty(value) || value == "()")
                return result;

            var entries = ReadUtil.GetParenthesisValueToArray(value);
            if (entries == null) return result;

            foreach (var entry in entries)
            {
                var parts = entry.Split(':');
                if (parts.Length != 2) continue;

                result.Add(new ItemRequirement
                {
                    itemId = parts[0].Trim(),
                    amount = int.Parse(parts[1].Trim())
                });
            }
            return result;
        }

        public string Write(object value)
        {
            var list = (List<ItemRequirement>)value;
            if (list == null || list.Count == 0) return "()";

            var parts = new List<string>();
            foreach (var req in list)
                parts.Add($"{req.itemId}:{req.amount}");

            return "(" + string.Join(",", parts) + ")";
        }
    }
}
