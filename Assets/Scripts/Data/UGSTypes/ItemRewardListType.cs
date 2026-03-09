using System.Collections.Generic;
using GoogleSheet.Type;

namespace GoogleSheet.Type
{
    // 시트 컬럼 타입: itemrewards
    // 셀 포맷 예시: (item_book:1,item_money:500)
    [Type(Type: typeof(List<ItemReward>), TypeName: new string[] { "itemrewards", "ItemRewards" })]
    public class ItemRewardListType : IType
    {
        public object DefaultValue => new List<ItemReward>();

        public object Read(string value)
        {
            var result = new List<ItemReward>();
            if (string.IsNullOrEmpty(value) || value == "()")
                return result;

            var entries = ReadUtil.GetParenthesisValueToArray(value);
            if (entries == null) return result;

            foreach (var entry in entries)
            {
                var parts = entry.Split(':');
                if (parts.Length != 2) continue;

                result.Add(new ItemReward
                {
                    itemId = parts[0].Trim(),
                    amount = int.Parse(parts[1].Trim())
                });
            }
            return result;
        }

        public string Write(object value)
        {
            var list = (List<ItemReward>)value;
            if (list == null || list.Count == 0) return "()";

            var parts = new List<string>();
            foreach (var reward in list)
                parts.Add($"{reward.itemId}:{reward.amount}");

            return "(" + string.Join(",", parts) + ")";
        }
    }
}
