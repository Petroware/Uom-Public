using System;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;

namespace Petroware.Uom
{
  /// <summary>
  ///   A collections of useful utilities and conveniences when working
  ///   with the standard JDK XML package.
  ///
  ///   This is a minimal excerpt from no.petroware.cc.util.XmlUtil.
  /// </summary>
  internal sealed class XmlUtil
  {
    /// <summary>
    ///   Private constructor to prevent client instantiation.
    /// </summary>
    private XmlUtil()
    {
      Debug.Assert(false, "This constructor should never be called");
    }

    /// <summary>
    ///   Return a specified child element from the given element.
    ///   Only intermediate children are considered.
    /// </summary>
    ///
    /// <param name="element">
    ///   Element to search. Non-null.
    /// </param>
    /// <param name="childName">
    ///   Name of child element to find. Non-null.
    /// </param>
    /// <returns>
    ///   The requested child element. If there are more than one child elements
    ///   with the same name, the first one encountered is returned.
    ///   Null if not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   If element or childName is null.
    /// </exception>
    public static XmlElement GetChild(XmlElement element, string childName)
    {
      if (element == null)
        throw new ArgumentNullException("element cannot be null");

      if (childName == null)
        throw new ArgumentNullException("childName cannot be null");

      for (int i = 0; i < element.ChildNodes.Count; i++) {
        XmlNode node = element.ChildNodes[i];
        if (node is XmlElement && node.Name.Equals(childName))
          return (XmlElement) node;
      }

      // Not found
      return null;
    }

    /// <summary>
    ///   Return all children elements of the given name from the specified element.
    ///   Search full depth.
    /// </summary>
    ///
    /// <param name="element">
    ///   Element to search. Non-null.
    /// </param>
    /// <param name="childName">
    ///   Name of child elements to find. Non-null.
    /// </param>
    /// <returns>
    ///   The requested child elements. Never null.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   If element or childName is null.
    /// </exception>
    public static List<XmlElement> FindChildren(XmlElement element, string childName)
    {
      if (element == null)
        throw new ArgumentNullException("element cannot be null");

      if (childName == null)
        throw new ArgumentNullException("childName cannot be null");

      XmlNodeList nodes = element.GetElementsByTagName(childName);
      List<XmlElement> elements = new List<XmlElement>();
      for (int i = 0; i < nodes.Count; i++)
        elements.Add((XmlElement) nodes[i]);

      return elements;
    }

    /// <summary>
    ///   Return the text content of the child of the specified element.
    /// </summary>
    ///
    /// <param name="element">
    ///   Parent element of child. Non-null.
    /// </param>
    /// <param name="childName">
    ///   Name of child element to get text content of. Non-null.
    /// </param>
    /// <param name="defaultValue">
    ///   Value to return if the child element is not found. May be null.
    /// </param>
    /// <returns>
    ///   The requested value, or the default value if not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   If element or childName is null.
    /// </exception>
    public static string GetChildValue(XmlElement element, string childName, string defaultValue)
    {
      if (element == null)
        throw new ArgumentNullException("element cannot be null");

      if (childName == null)
        throw new ArgumentNullException("childName cannot be null");

      XmlElement childElement = GetChild(element, childName);
      return childElement != null ? childElement.InnerText.Trim() : defaultValue;
    }

    /// <summary>
    ///   Return the attribute of a specified element as a string.
    /// </summary>
    ///
    /// <param name="element">
    ///   Element to find attribute of. Non-null.
    /// </param>
    /// <param name="attributeName">
    ///   Name of attribute to get value of. Non-null.
    /// </param>
    /// <param name="defaultValue">
    ///   Default value to report if attribute is not found. May be null.
    /// </param>
    /// <returns>
    ///   The requested value or defaultValue if not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   If element or attributeName is null.
    /// </exception>
    public static string GetAttribute(XmlElement element, string attributeName, string defaultValue)
    {
      if (element == null)
        throw new ArgumentNullException("element cannot be null");

      if (attributeName == null)
        throw new ArgumentNullException("attributeName cannot be null");

      string text = element.GetAttribute(attributeName);
      return text != null && text.Trim() != string.Empty ? text : defaultValue;
    }
  }
}
