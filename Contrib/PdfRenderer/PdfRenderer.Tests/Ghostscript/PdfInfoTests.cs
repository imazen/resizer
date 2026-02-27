using System.IO;
using System.Xml.Serialization;
using Xunit;
using ImageResizer.Plugins.PdfRenderer.Ghostscript;

namespace ImageResizer.Plugins.Pdf.Tests.Ghostscript
{
    public class PdfInfoTests
    {
        [Fact]
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
            XmlSerializer serializer = new XmlSerializer(typeof(PdfInfo));

            // Act
            PdfInfo pdfInfo;
            using(StringReader reader = new StringReader(pdfInfoXml))
            {
                pdfInfo = (PdfInfo)serializer.Deserialize(reader);
            }

            // Assert
            Assert.Equal("<F>", pdfInfo.FileName);
            Assert.Equal("\"T\"", pdfInfo.Title);
            Assert.Equal("'A'", pdfInfo.Author);
            Assert.Equal("&S&", pdfInfo.Subject);
            Assert.Equal("K", pdfInfo.Keywords);
            Assert.Equal("C", pdfInfo.Creator);
            Assert.Equal("P", pdfInfo.Producer);
            Assert.Equal("Cd", pdfInfo.CreationDate);
            Assert.Equal("Md", pdfInfo.ModifiedDate);
            Assert.Equal(2, pdfInfo.PageCount);
            Assert.Equal(2, pdfInfo.Pages.Count);
            // - Pages[0]
            Assert.Equal(1, pdfInfo.Pages[0].Number);
            Assert.Equal(0, pdfInfo.Pages[0].Rotate);
            Assert.False(pdfInfo.Pages[0].Transparency);
            // - Pages[0].MediaBox
            Assert.NotNull(pdfInfo.Pages[0].MediaBox);
            Assert.Equal(111.1, pdfInfo.Pages[0].MediaBox.Left, 4);
            Assert.Equal(112.2, pdfInfo.Pages[0].MediaBox.Top, 4);
            Assert.Equal(113.3, pdfInfo.Pages[0].MediaBox.Width, 4);
            Assert.Equal(114.4, pdfInfo.Pages[0].MediaBox.Height, 4);
            // - Pages[0].CropBox
            Assert.NotNull(pdfInfo.Pages[0].CropBox);
            Assert.Equal(121.1, pdfInfo.Pages[0].CropBox.Left, 4);
            Assert.Equal(122.2, pdfInfo.Pages[0].CropBox.Top, 4);
            Assert.Equal(123.3, pdfInfo.Pages[0].CropBox.Width, 4);
            Assert.Equal(124.4, pdfInfo.Pages[0].CropBox.Height, 4);
            // - Pages[1]
            Assert.Equal(2, pdfInfo.Pages[1].Number);
            Assert.Equal(90, pdfInfo.Pages[1].Rotate);
            Assert.True(pdfInfo.Pages[1].Transparency);
            // - Pages[1].MediaBox
            Assert.NotNull(pdfInfo.Pages[1].MediaBox);
            Assert.Equal(211.1, pdfInfo.Pages[1].MediaBox.Left, 4);
            Assert.Equal(212.2, pdfInfo.Pages[1].MediaBox.Top, 4);
            Assert.Equal(213.3, pdfInfo.Pages[1].MediaBox.Width, 4);
            Assert.Equal(214.4, pdfInfo.Pages[1].MediaBox.Height, 4);
            // - Pages[1].CropBox
            Assert.NotNull(pdfInfo.Pages[1].CropBox);
            Assert.Equal(221.1, pdfInfo.Pages[1].CropBox.Left, 4);
            Assert.Equal(222.2, pdfInfo.Pages[1].CropBox.Top, 4);
            Assert.Equal(223.3, pdfInfo.Pages[1].CropBox.Width, 4);
            Assert.Equal(224.4, pdfInfo.Pages[1].CropBox.Height, 4);
        }
    }
}
