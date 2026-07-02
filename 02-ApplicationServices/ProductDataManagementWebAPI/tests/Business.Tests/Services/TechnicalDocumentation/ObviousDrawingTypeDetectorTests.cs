using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class ObviousDrawingTypeDetectorTests
{
    [Theory]
    [InlineData("K-02_lista_pretow_dolne.pdf", "zbrojenie_stropu_dolne")]
    [InlineData("K-03_lista_pretow_gorne.pdf", "zbrojenie_stropu_gorne")]
    [InlineData("K-04_lista_drewna.pdf", "rzut_wiezby_dachowej")]
    [InlineData("ARCH-02_zestawienie_pomieszczen_parter.pdf", "rzut_parteru")]
    [InlineData("K-01_rzut_fundamentow.pdf", "rzut_fundamentow")]
    [InlineData("ZAGOSPODAROWANIE_DZIAŁKI.jpg", "zagospodarowanie_terenu")]
    public void TryDetect_returnsObviousDrawingType_fromFileName(string fileName, string expectedType)
    {
        // Act
        DrawingClassification? result = ObviousDrawingTypeDetector.TryDetect(fileName);

        // Assert
        result.Should().NotBeNull();
        result!.DrawingType.Should().Be(expectedType);
    }

    [Fact]
    public void TryDetect_returnsNull_forAmbiguousFileName()
    {
        // Act
        DrawingClassification? result = ObviousDrawingTypeDetector.TryDetect("scan_page_7.pdf");

        // Assert
        result.Should().BeNull();
    }
}
