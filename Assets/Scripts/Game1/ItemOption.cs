
namespace Game1
{
    public class ItemOption
    {
    	public int param;
    
    	public sbyte active;
    
    	public sbyte activeCard;
    
    	public ItemOptionTemplate optionTemplate;
    
    	public ItemOption()
    	{
    	}
    
    	public bool IsValidOption()
    	{
    		if (this != null && optionTemplate != null && optionTemplate.id >= 0 && optionTemplate.id != 21 && optionTemplate.id != 200 && optionTemplate.id != 72 && optionTemplate.id != 57 && optionTemplate.id != 58 && optionTemplate.id != 34 && optionTemplate.id != 35 && optionTemplate.id != 36 && optionTemplate.id != 102 && optionTemplate.id != 107)
    		{
    			return true;
    		}
    		return false;
    	}
    
    	public ItemOption(int optionTemplateId, int param)
    	{
    		if (optionTemplateId == 22)
    		{
    			optionTemplateId = 6;
    			param = ScaleParam(param, 1000);
    		}
    		if (optionTemplateId == 23)
    		{
    			optionTemplateId = 7;
    			param = ScaleParam(param, 1000);
    		}
    		this.param = param;
    		optionTemplate = GetOptionTemplate(optionTemplateId);
    	}

    	public string getOptionString()
    	{
    		if (optionTemplate == null || optionTemplate.name == null)
    		{
    			return string.Empty;
    		}
    		return NinjaUtil.Replace(optionTemplate.name, "#", param + string.Empty);
    	}

    	public string getOptiongColor()
    	{
    		if (optionTemplate == null || optionTemplate.name == null)
    		{
    			return string.Empty;
    		}
    		return NinjaUtil.Replace(optionTemplate.name, "$", string.Empty);
    	}

    	private static ItemOptionTemplate GetOptionTemplate(int optionTemplateId)
    	{
    		ItemOptionTemplate[] iOptionTemplates = GameScr.gI().iOptionTemplates;
    		if (iOptionTemplates != null && optionTemplateId >= 0 && optionTemplateId < iOptionTemplates.Length && iOptionTemplates[optionTemplateId] != null)
    		{
    			return iOptionTemplates[optionTemplateId];
    		}
    		Res.err("Missing item option template id: " + optionTemplateId);
    		return new ItemOptionTemplate
    		{
    			id = -1,
    			name = string.Empty,
    			type = 0
    		};
    	}

    	private static int ScaleParam(int param, int multiplier)
    	{
    		long num = (long)param * (long)multiplier;
    		if (num > int.MaxValue)
    		{
    			return int.MaxValue;
    		}
    		if (num < int.MinValue)
    		{
    			return int.MinValue;
    		}
    		return (int)num;
    	}
    }
}
