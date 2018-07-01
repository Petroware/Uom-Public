package no.petroware.uom;

import java.util.ArrayList;
import java.util.List;
import java.io.InputStream;
import java.io.IOException;

import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.ParserConfigurationException;

import org.w3c.dom.Document;
import org.w3c.dom.Element;
import org.w3c.dom.Node;
import org.w3c.dom.NodeList;

import org.xml.sax.SAXException;

/**
 * A collections of useful utilities and conveniences when working
 * with the standard JDK XML package.
 * <p>
 * This is a minimal excerpt from no.petroware.cc.util.XmlUtil.
 *
 * @author <a href="mailto:info@petroware.no">Petroware AS</a>
 */
final class XmlUtil
{
  /**
   * Private constructor to prevent client instantiation.
   */
  private XmlUtil()
  {
    assert false : "This constructor should never be called";
  }

  /**
   * Create an XML document from the specified input stream.
   *
   * @param inputStream  Input stream to create document from. Non-null.
   * @return             The requested document. Never null.
   * @throws IllegalArgumentException  If inputStream is null.
   * @throws IOException   If the fail cannot be accessed for some reason.
   * @throws SAXException  If the file doesn't contain a proper XML document.
   */
  public static Document newDocument(InputStream inputStream)
    throws IOException, SAXException
  {
    if (inputStream == null)
      throw new IllegalArgumentException("inputStream cannot be null");

    DocumentBuilderFactory documentBuilderFactory = DocumentBuilderFactory.newInstance();

    try {
      DocumentBuilder documentBuilder = documentBuilderFactory.newDocumentBuilder();
      return documentBuilder.parse(inputStream);
    }
    catch (ParserConfigurationException exception) {
      throw new SAXException(exception);
    }
  }

  /**
   * Return a specified child element from the given element.
   * Only intermediate children are considered.
   *
   * @param element    Element to search. Non-null.
   * @param childName  Name of child element to find. Non-null.
   * @return           The requested child element. If there are more than one child elements
   *                   with the same name, the first one encountered is returned.
   *                   Null if not found.
   * @throws IllegalArgumentException  If element or childName is null.
   */
  public static Element getChild(Element element, String childName)
  {
    if (element == null)
      throw new IllegalArgumentException("element cannot be null");

    if (childName == null)
      throw new IllegalArgumentException("childName cannot be null");

    for (Node child = element.getFirstChild(); child != null; child = child.getNextSibling())
      if (child instanceof Element && childName.equals(child.getNodeName()))
        return (Element) child;

    // Not found
    return null;
  }

  /**
   * Return all children elements of the given name from the specified element.
   * Search full depth.
   *
   * @param element    Element to search. Non-null.
   * @param childName  Name of child elements to find. Non-null.
   * @return           The requested child elements. Never null.
   * @throws IllegalArgumentException  If element or childName is null.
   */
  public static List<Element> findChildren(Element element, String childName)
  {
    if (element == null)
      throw new IllegalArgumentException("element cannot be null");

    if (childName == null)
      throw new IllegalArgumentException("childName cannot be null");

    List<Element> elements = new ArrayList<>();

    NodeList nodeList = element.getElementsByTagName(childName);
    for (int i = 0; i < nodeList.getLength(); i++)
      elements.add((Element) nodeList.item(i));

    return elements;
  }

  /**
   * Return the text content of the child of the specified element.
   *
   * @param element       Parent element of child. Non-null.
   * @param childName     Name of child element to get text content of. Non-null.
   * @param defaultValue  Value to return if the child element is not found. May be null.
   * @return              The requested value, or the default value if not found.
   * @throws IllegalArgumentException  If element or childName is null.
   */
  public static String getChildValue(Element element, String childName, String defaultValue)
  {
    if (element == null)
      throw new IllegalArgumentException("element cannot be null");

    if (childName == null)
      throw new IllegalArgumentException("childName cannot be null");

    Element childElement = getChild(element, childName);
    return childElement != null ? childElement.getTextContent().trim() : defaultValue;
  }

  /**
   * Return the attribute of a specified element as a string.
   *
   * @param element        Element to find attribute of. Non-null.
   * @param attributeName  Name of attribute to get value of.
   * @param defaultValue   Default value to report if attribute is not found.
   * @return               The requested value or defaultValue if not found.
   */
  public static String getAttribute(Element element, String attributeName, String defaultValue)
  {
    if (element == null)
      throw new IllegalArgumentException("element cannot be null");

    if (attributeName == null)
      throw new IllegalArgumentException("attributeName cannot be null");

    String text = element.getAttribute(attributeName);
    return text != null && !text.trim().isEmpty() ? text : defaultValue;
  }
}
