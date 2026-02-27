using System;
using Xunit;
using ImageResizer.Plugins.DiskCache;

namespace ImageResizer.Plugins.DiskCache.Tests {
    public class CleanupStrategyTests {

        [Fact]
        public void DefaultConstructor_HasExpectedDefaults() {
            var strategy = new CleanupStrategy();
            Assert.Equal(new TimeSpan(0, 5, 0), strategy.StartupDelay);
            Assert.Equal(new TimeSpan(0, 0, 20), strategy.MinDelay);
            Assert.Equal(new TimeSpan(0, 5, 0), strategy.MaxDelay);
            Assert.Equal(new TimeSpan(0, 0, 4), strategy.OptimalWorkSegmentLength);
            Assert.Equal(400, strategy.TargetItemsPerFolder);
            Assert.Equal(1000, strategy.MaximumItemsPerFolder);
            Assert.Equal(new TimeSpan(96, 0, 0), strategy.AvoidRemovalIfUsedWithin);
            Assert.Equal(new TimeSpan(24, 0, 0), strategy.AvoidRemovalIfCreatedWithin);
            Assert.Equal(new TimeSpan(0, 5, 0), strategy.ProhibitRemovalIfUsedWithin);
            Assert.Equal(new TimeSpan(0, 10, 0), strategy.ProhibitRemovalIfCreatedWithin);
        }

        [Fact]
        public void Properties_SetGet() {
            var strategy = new CleanupStrategy();
            strategy.StartupDelay = TimeSpan.FromMinutes(10);
            strategy.MinDelay = TimeSpan.FromSeconds(30);
            strategy.MaxDelay = TimeSpan.FromMinutes(10);
            strategy.OptimalWorkSegmentLength = TimeSpan.FromSeconds(8);
            strategy.TargetItemsPerFolder = 200;
            strategy.MaximumItemsPerFolder = 500;

            Assert.Equal(TimeSpan.FromMinutes(10), strategy.StartupDelay);
            Assert.Equal(TimeSpan.FromSeconds(30), strategy.MinDelay);
            Assert.Equal(TimeSpan.FromMinutes(10), strategy.MaxDelay);
            Assert.Equal(TimeSpan.FromSeconds(8), strategy.OptimalWorkSegmentLength);
            Assert.Equal(200, strategy.TargetItemsPerFolder);
            Assert.Equal(500, strategy.MaximumItemsPerFolder);
        }

        [Fact]
        public void MeetsCleanupCriteria_OldFile_ReturnsTrue() {
            var strategy = new CleanupStrategy();
            var oldDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(10));
            var info = new CachedFileInfo(oldDate, oldDate, oldDate);
            Assert.True(strategy.MeetsCleanupCriteria(info));
        }

        [Fact]
        public void MeetsCleanupCriteria_RecentFile_ReturnsFalse() {
            var strategy = new CleanupStrategy();
            var recentDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1));
            var info = new CachedFileInfo(recentDate, recentDate, recentDate);
            Assert.False(strategy.MeetsCleanupCriteria(info));
        }

        [Fact]
        public void MeetsOverMaxCriteria_VeryRecentFile_ReturnsFalse() {
            var strategy = new CleanupStrategy();
            var recentDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1));
            var info = new CachedFileInfo(recentDate, recentDate, recentDate);
            Assert.False(strategy.MeetsOverMaxCriteria(info));
        }

        [Fact]
        public void MeetsOverMaxCriteria_OldFile_ReturnsTrue() {
            var strategy = new CleanupStrategy();
            var oldDate = DateTime.UtcNow.Subtract(TimeSpan.FromHours(1));
            var info = new CachedFileInfo(oldDate, oldDate, oldDate);
            Assert.True(strategy.MeetsOverMaxCriteria(info));
        }

        [Fact]
        public void ShouldRemove_OverMax_UsesOverMaxCriteria() {
            var strategy = new CleanupStrategy();
            var recentDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1));
            var recentFile = new CachedFileInfo(recentDate, recentDate, recentDate);
            // Recent file should not be removed even when over max
            Assert.False(strategy.ShouldRemove("test.jpg", recentFile, true));
        }

        [Fact]
        public void ShouldRemove_NotOverMax_UsesCleanupCriteria() {
            var strategy = new CleanupStrategy();
            var oldDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(10));
            var oldFile = new CachedFileInfo(oldDate, oldDate, oldDate);
            Assert.True(strategy.ShouldRemove("test.jpg", oldFile, false));
        }

        [Fact]
        public void GetIssues_DefaultSettings_NoWarnings() {
            var strategy = new CleanupStrategy();
            var issues = strategy.GetIssues();
            int count = 0;
            foreach (var issue in issues) count++;
            Assert.Equal(0, count);
        }

        [Fact]
        public void GetIssues_ModifiedSettings_ReportsWarning() {
            var strategy = new CleanupStrategy();
            strategy.TargetItemsPerFolder = 50; // Changed from default 400
            var issues = strategy.GetIssues();
            int count = 0;
            foreach (var issue in issues) count++;
            Assert.True(count > 0, "Should report warning when settings are changed from defaults");
        }
    }
}
