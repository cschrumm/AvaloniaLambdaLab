namespace Service.Library;

public static class Utils
{
    
    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }
    
    /*
     * An extension method to set a property value by name
     */
    public static void SetPropertyValue(this object obj, string propertyName, object other)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        var propOther = other.GetType().GetProperty(propertyName);
        if (prop != null && prop.CanWrite && propOther != null && propOther.CanRead)
        {
            if (prop.PropertyType == propOther.PropertyType)
            {
                var value = propOther.GetValue(other);
                prop.SetValue(obj, value);
            }
        }
        
    }
    
    /*
     *
     * Given propery name copy source to target object
     * using reflection check the type is the same
     */
    public static void CopyProperty(string propertyName, object source, object target, bool ThrowOnError = false)
    {
        var sourceProp = source.GetType().GetProperty(propertyName);
        var targetProp = target.GetType().GetProperty(propertyName);
        if (sourceProp == null || targetProp == null)
        {
            if (ThrowOnError) throw new Exception("Property not found");
            return;
        }

        if (sourceProp.PropertyType != targetProp.PropertyType)
        {
            if (ThrowOnError) throw new Exception("Property type mismatch");
            return;
        } 
         
        var value = sourceProp.GetValue(source);
        targetProp.SetValue(target, value);
        
    }
    
    // copy the properties that match from source to target
    public static void CopyProperties(object source, object target, bool ThrowOnError = false)
    {
        foreach (var sourceProp in source.GetType().GetProperties())
        {
            foreach (var targetProp in target.GetType().GetProperties())
            {
                if (sourceProp.PropertyType == targetProp.PropertyType &&
                    sourceProp.Name == targetProp.Name)
                {
                    var value = sourceProp.GetValue(source);
                    targetProp.SetValue(target, value);
                }
            }
        }
    }
}