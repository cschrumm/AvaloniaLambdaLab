using System.Collections.ObjectModel;
using System.Runtime.InteropServices.JavaScript;

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


    public static bool SyncronizeCollections<T>(this IList<T> dest_collection, IList<T> source_collection,
        Func<T, T, bool> itemEqual,
        Action<T,T>? updateItem = null)
    {
        var changed = false;
        // Remove items not in source
        var tmp = new List<T>();
        
        foreach (var sr in dest_collection)
        {
            bool found = false;
            foreach (var ds in source_collection)
            {
                if (itemEqual(ds, sr))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                changed = true;
                tmp.Add(sr);
            }
        }

        foreach (var rem in tmp)
        {
            dest_collection.Remove(rem);
        }

        // Add items from source not in collection
        foreach (var ds in source_collection)
        {
            bool found = false;
            foreach (var cl in dest_collection)
            {
                if(itemEqual(cl, ds))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                changed = true;
                dest_collection.Add(ds);
            }
        }
        
        // Update existing items
        foreach (var ds in source_collection)
        {
            foreach (var cl in dest_collection)
            {
                if (itemEqual(cl, ds))
                {
                    updateItem?.Invoke(cl, ds);
                    break;
                }
            }
        }

        return changed;


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