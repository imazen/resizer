using System.IO;
using System.Xml.Serialization;
using MbUnit.Framework;
using ImageResizer.Plugins.PdfRenderer.Ghostscript;

namespace ImageResizer.Plugins.Pdf.Tests.Ghostscript
{
    public class PdfInfoTests
    {
        [Test]
        public void ExpectDeserialize()
        {
            // Arrange
            const string pdfInfoXml = "<pdf>" +
                                      "  <fileNameData>" +
                                      "    <value>60</value>" + // <
                                      "    <value>70</value>" + // F
                                      "    <value>62</value>" + // >
                                      "  </fileNameData>" + 
                                      "  <titleData>" +
                                      "    <value>34</value>" + // "
                                      "    <value>84</value>" + // T
                                      "    <value>34</value>" + // "
                                      "  </titleData>" + 
                                      "  <authorData>" +
                                      "    <value>39</value>" + // '
                                      "    <value>65</value>" + // A
                                      "    <value>39</value>" + // '
                                      "  </authorData>" +
                                      "  <subjectData>" +
                                      "    <value>38</value>" + // &
                                      "    <value>83</value>" + // S
                                      "    <value>38</value>" + // &
                                      "  </subjectData>" +
                                      "  <keywordsData>" +
                                      "    <value>254</value>" +
                                      "    <value>255</value>" +
                                      "    <value>0</value>" + // K (UTF16 BE)
                                      "    <value>75</value>" +
                                      "  </keywordsData>" +
                                      "  <creatorData>" +
                                      "    <value>255</value>" +
                                      "    <value>254</value>" +
                                      "    <value>67</value>" + // C (UTF16 LE)
                                      "    <value>0</value>" +
                                      "  </creatorData>" +
                                      "  <producerData>" +
                                      "    <value>255</value>" +
                                      "    <value>254</value>" +
                                      "    <value>0</value>" +
                                      "    <value>0</value>" +
                                      "    <value>80</value>" + // P (UTF32 LE)
                                      "    <value>0</value>" +
                                      "    <value>0</value>" +
                                      "    <value>00</value>" +
                                      "  </producerData>" +
                                      "  <creationDateData>" +
                                      "    <value>239</value>" +
                                      "    <value>187</value>" +
                                      "    <value>191</value>" +
                                      "    <value>67</value>" + // C (UTF8)
                                      "    <value>100</value>" + // d
                                      "  </creationDateData>" +
                                      "  <modifiedDateData>" + 
                                      "    <value>77</value>" + // M (ASCII)
                                      "    <value>100</value>" + // d
                                      "  </modifiedDateData>" +
                                      "  <pageCount>2</pageCount>" +
                                      "  <pages>" +
                                      "    <page number=\"1\">" +
                                      "      <mediaBox left=\"111.1\" top=\"112.2\" width=\"113.3\" height=\"114.4\" />" +
                                      "      <cropBox left=\"121.1\" top=\"122.2\" width=\"123.3\" height=\"124.4\"/>" +
                                      "    </page>" +
                                      "    <page number=\"2\" rotate=\"90\" transparency=\"true\">" +
                                      "      <mediaBox left=\"211.1\" top=\"212.2\" width=\"213.3\" height=\"214.4\" />" +
                                      "      <cropBox left=\"221.1\" top=\"222.2\" width=\"223.3\" height=\"224.4\"/>" +
                                      "    </page>" +
                                      "  </pages>" +
                                      "</pdf>";
            const double delta = 0.0001;
            XmlSerializer serializer = new XmlSerializer(typeof(PdfInfo));

            // Act
            PdfInfo pdfInfo;
            using(StringReader reader = new StringReader(pdfInfoXml))
            {
                pdfInfo = (PdfInfo)serializer.Deserialize(reader);
            }

            // Assert
            Assert.AreEqual("<F>", pdfInfo.FileName);
            Assert.AreEqual("\"T\"", pdfInfo.Title);
            Assert.AreEqual("'A'", pdfInfo.Author);
            Assert.AreEqual("&S&", pdfInfo.Subject);
            Assert.AreEqual("K", pdfInfo.Keywords);
            Assert.AreEqual("C", pdfInfo.Creator);
            Assert.AreEqual("P", pdfInfo.Producer);
            Assert.AreEqual("Cd", pdfInfo.CreationDate);
            Assert.AreEqual("Md", pdfInfo.ModifiedDate);
            Assert.AreEqual(2, pdfInfo.PageCount);
            Assert.AreEqual(2, pdfInfo.Pages.Count);
            // - Pages[0]
            Assert.AreEqual(1, pdfInfo.Pages[0].Number);
            Assert.AreEqual(0, pdfInfo.Pages[0].Rotate);
            Assert.IsFalse(pdfInfo.Pages[0].Transparency);
            // - Pages[0].MediaBox
            Assert.IsNotNull(pdfInfo.Pages[0].MediaBox);
            Assert.AreApproximatelyEqual(111.1, pdfInfo.Pages[0].MediaBox.Left, delta);
            Assert.AreApproximatelyEqual(112.2, pdfInfo.Pages[0].MediaBox.Top, delta);
            Assert.AreApproximatelyEqual(113.3, pdfInfo.Pages[0].MediaBox.Width, delta);
            Assert.AreApproximatelyEqual(114.4, pdfInfo.Pages[0].MediaBox.Height, delta);
            // - Pages[0].CropBox
            Assert.IsNotNull(pdfInfo.Pages[0].CropBox);
            Assert.AreApproximatelyEqual(121.1, pdfInfo.Pages[0].CropBox.Left, delta);
            Assert.AreApproximatelyEqual(122.2, pdfInfo.Pages[0].CropBox.Top, delta);
            Assert.AreApproximatelyEqual(123.3, pdfInfo.Pages[0].CropBox.Width, delta);
            Assert.AreApproximatelyEqual(124.4, pdfInfo.Pages[0].CropBox.Height, delta);
            // - Pages[1]
            Assert.AreEqual(2, pdfInfo.Pages[1].Number);
            Assert.AreEqual(90, pdfInfo.Pages[1].Rotate);
            Assert.IsTrue(pdfInfo.Pages[1].Transparency);
            // - Pages[1].MediaBox
            Assert.IsNotNull(pdfInfo.Pages[1].MediaBox);
            Assert.AreApproximatelyEqual(211.1, pdfInfo.Pages[1].MediaBox.Left, delta);
            Assert.AreApproximatelyEqual(212.2, pdfInfo.Pages[1].MediaBox.Top, delta);
            Assert.AreApproximatelyEqual(213.3, pdfInfo.Pages[1].MediaBox.Width, delta);
            Assert.AreApproximatelyEqual(214.4, pdfInfo.Pages[1].MediaBox.Height, delta);
            // - Pages[1].CropBox
            Assert.IsNotNull(pdfInfo.Pages[1].CropBox);
            Assert.AreApproximatelyEqual(221.1, pdfInfo.Pages[1].CropBox.Left, delta);
            Assert.AreApproximatelyEqual(222.2, pdfInfo.Pages[1].CropBox.Top, delta);
            Assert.AreApproximatelyEqual(223.3, pdfInfo.Pages[1].CropBox.Width, delta);
            Assert.AreApproximatelyEqual(224.4, pdfInfo.Pages[1].CropBox.Height, delta);
        }
    }
}
