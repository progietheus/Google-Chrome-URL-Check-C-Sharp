# Google-Chrome-URL-Check-C-Sharp

## Check all open instances of Google Chrome on a machine for a specified URL (C#)

After trying to manually traverse the AutomationElements trees to find the URL as presented in other [solutions found online](https://stackoverflow.com/questions/18897070/getting-the-current-tabs-url-from-google-chrome-using-c-sharp), I came up with a function to search the root AutomationElement beginning at the browser for the encasing ControlType of "edit", which contains the URL we are looking for. 
```c#
private static List<AutomationElement> GetEditElement(AutomationElement rootElement, List<AutomationElement> ret)
{
    Condition isControlElementProperty = new PropertyCondition(AutomationElement.IsControlElementProperty, true);
    Condition isEnabledProperty = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
    TreeWalker walker = new TreeWalker(new AndCondition(isControlElementProperty, isEnabledProperty));
    AutomationElement elementNode = walker.GetFirstChild(rootElement);
    while (elementNode != null)
    {
        if (elementNode.Current.ControlType.LocalizedControlType == "edit")
            ret.Add(elementNode);
        GetEditElement(elementNode, ret);
        elementNode = walker.GetNextSibling(elementNode);
    }
    return ret;
}
```
You can just return the result to a AutomationElement value like this
```c#
AutomationElement elmUrlBar = GetEditElement(elm1, ret)[0];
```

Then retrieve the URL 
```c#
var result = ((ValuePattern)elmUrlBar.GetCurrentPattern(patterns[0])).Current.Value;
```

## References needed
Add the following DLLs to your project to fix compile errors: UIAutomationClient and UIAutomationTypes.

Hopefully some of you will find this useful.
