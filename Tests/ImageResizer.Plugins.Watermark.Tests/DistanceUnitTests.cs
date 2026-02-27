using System;
using Xunit;
using ImageResizer.Plugins.Watermark;

namespace ImageResizer.Plugins.Watermark.Tests {
    public class DistanceUnitTests {

        // --- TryParse ---

        [Fact]
        public void TryParse_Null_ReturnsNull() {
            Assert.Null(DistanceUnit.TryParse(null));
        }

        [Fact]
        public void TryParse_Empty_ReturnsNull() {
            Assert.Null(DistanceUnit.TryParse(""));
        }

        [Fact]
        public void TryParse_InvalidText_ReturnsNull() {
            Assert.Null(DistanceUnit.TryParse("abc"));
        }

        [Theory]
        [InlineData("50px", 50.0, DistanceUnit.Units.Pixels)]
        [InlineData("100px", 100.0, DistanceUnit.Units.Pixels)]
        [InlineData("0px", 0.0, DistanceUnit.Units.Pixels)]
        [InlineData("-10px", -10.0, DistanceUnit.Units.Pixels)]
        public void TryParse_PixelValues(string input, double expectedValue, DistanceUnit.Units expectedType) {
            var result = DistanceUnit.TryParse(input);
            Assert.NotNull(result);
            Assert.Equal(expectedValue, result.Value);
            Assert.Equal(expectedType, result.Type);
        }

        [Theory]
        [InlineData("50%", 50.0, DistanceUnit.Units.Percentage)]
        [InlineData("100%", 100.0, DistanceUnit.Units.Percentage)]
        [InlineData("0%", 0.0, DistanceUnit.Units.Percentage)]
        public void TryParse_PercentageValues(string input, double expectedValue, DistanceUnit.Units expectedType) {
            var result = DistanceUnit.TryParse(input);
            Assert.NotNull(result);
            Assert.Equal(expectedValue, result.Value);
            Assert.Equal(expectedType, result.Type);
        }

        [Fact]
        public void TryParse_PercentSuffix() {
            var result = DistanceUnit.TryParse("75percent");
            Assert.NotNull(result);
            Assert.Equal(75.0, result.Value);
            Assert.Equal(DistanceUnit.Units.Percentage, result.Type);
        }

        [Fact]
        public void TryParse_PctSuffix() {
            var result = DistanceUnit.TryParse("25pct");
            Assert.NotNull(result);
            Assert.Equal(25.0, result.Value);
            Assert.Equal(DistanceUnit.Units.Percentage, result.Type);
        }

        [Fact]
        public void TryParse_BareNumber_IsPixels() {
            var result = DistanceUnit.TryParse("42");
            Assert.NotNull(result);
            Assert.Equal(42.0, result.Value);
            Assert.Equal(DistanceUnit.Units.Pixels, result.Type);
        }

        [Fact]
        public void TryParse_DecimalValue() {
            var result = DistanceUnit.TryParse("10.5px");
            Assert.NotNull(result);
            Assert.Equal(10.5, result.Value);
        }

        // --- Constructor ---

        [Fact]
        public void Constructor_ValuesSet() {
            var unit = new DistanceUnit(50, DistanceUnit.Units.Percentage);
            Assert.Equal(50, unit.Value);
            Assert.Equal(DistanceUnit.Units.Percentage, unit.Type);
        }

        [Fact]
        public void StringConstructor_ValidInput() {
            var unit = new DistanceUnit("100px");
            Assert.Equal(100, unit.Value);
            Assert.Equal(DistanceUnit.Units.Pixels, unit.Type);
        }

        [Fact]
        public void StringConstructor_InvalidInput_Throws() {
            Assert.Throws<ArgumentException>(() => new DistanceUnit("invalid"));
        }

        // --- ToString ---

        [Fact]
        public void ToString_Pixels() {
            var unit = new DistanceUnit(50, DistanceUnit.Units.Pixels);
            Assert.Equal("50px", unit.ToString());
        }

        [Fact]
        public void ToString_Percentage() {
            var unit = new DistanceUnit(75, DistanceUnit.Units.Percentage);
            Assert.Equal("75percent", unit.ToString());
        }

        [Fact]
        public void ToString_DecimalPixels() {
            var unit = new DistanceUnit(10.5, DistanceUnit.Units.Pixels);
            Assert.Equal("10.5px", unit.ToString());
        }
    }
}
