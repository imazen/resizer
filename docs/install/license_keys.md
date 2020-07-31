

# Installing a license key

License keys are only used for paid/AGPL plugins, such as those exclusive to Performance, Creative, or Elite editions.

Publicly available binaries (such as those on nuget.org) require license keys. Confirmed AGPL compliant users, Elite and Support Contact customers can access binaries (via myget) that have no license key enforcement. 

After adding your license key to web.config, visit the diagnostics page, /resizer.debug on your web app to ensure it has been accepted. License key validation is done offline using public-key cryptography; it does not use the network.

The diagnostics page should list the domain license you have installed - or, if you have errors in your configuration - it should explain what is wrong. 

### Where does the license key go? 

```
<configuration>
  <resizer>
    <licenses>
      <license>
        [paste license here - one per license element]
      </license>
    </licenses>
  </resizer>
</configuration>
```

### What about staging and development? 

`<maphost from="" to="" />` allows you to tell ImageResizer to treat requests to a local hostname or IP address specified in `from` as if they were arriving with the HOST header specified in `to`. This lets you accurately verify licensing is working prior to deployment.

### What if I use a license-enabled .dll without a license?

All functionality will continue to work, but newly generated images will have a red dot in one corner. 

### Full example

```
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <configSections>
    <section name="resizer" type="ImageResizer.ResizerSection,ImageResizer"  requirePermission="false"  />
  </configSections>

  <resizer>
    <pipeline fakeExtensions=".ashx" />

    <plugins>
      <add name="MvcRoutingShim" />
      <add name="DiskCache" />
      <add name="PrettyGifs" />
      <add name="S3Reader" />
    </plugins>  

    <licenses>
      <maphost from="localhost" to="resizer.apphb.com" />
      <maphost from="stagingserver.local" to="resizer.apphb.com" />

      <license>
        resizer.apphb.com(R4Performance includes R4Performance):RG9tYWluOiByZXNpemVyLmFwcGhiLmNvbQpPd25lcjogTmF0aGFuYWVsIEpvbmVzCklzc3VlZDogMjAxNS0wNS0wMVQxNTowNzo1NloKRmVhdHVyZXM6IFI0UGVyZm9ybWFuY2U=:oWv2YlAkzTEWcaJ6fPMEsweTNh9Bt5evhjWVNHuXtiRNl22sSS3OB/XE69NsSx8kEs1ExSwzvjwPx95paQyxGsTDigdh/UCkh7TCUyIECX7pI2JtA5f3KkFzfwmISIE8d14Kyf3ijO6s2HI1A1obbH5IucyaDJLQBCSrykxJK6JM4NOM82UbAUfwXRCnjWw2frwtBDp9rezJ46iQ80BXxTJ1LXlSqBry5z7bdSZtcP2k8L+Zp3t+9Blfl2k6z0um06kDa7RkPnmfwKCYTU+HbPQ2qDfGvcNaRC6XEa17ztTn52T6hErS7AJKIZ4OKxvw3olLmmVjEg+LiuKo7NVmmQ==
      </license>

      <license>
        domain.com(R4Performance includes R4Performance):RG9tYWluOiByZXNpemVyLmFwcGhiLmNvbQpPd25lcjogTmF0aGFuYWVsIEpvbmVzCklzc3VlZDogMjAxNS0wNS0wMVQxNTowNzo1NloKRmVhdHVyZXM6IFI0UGVyZm9ybWFuY2U=:oWv2YlAkzTEWcaJ6fPMEsweTNh9Bt5evhjWVNHuXtiRNl22sSS3OB/XE69NsSx8kEs1ExSwzvjwPx95paQyxGsTDigdh/UCkh7TCUyIECX7pI2JtA5f3KkFzfwmISIE8d14Kyf3ijO6s2HI1A1obbH5IucyaDJLQBCSrykxJK6JM4NOM82UbAUfwXRCnjWw2frwtBDp9rezJ46iQ80BXxTJ1LXlSqBry5z7bdSZtcP2k8L+Zp3t+9Blfl2k6z0um06kDa7RkPnmfwKCYTU+HbPQ2qDfGvcNaRC6XEa17ztTn52T6hErS7AJKIZ4OKxvw3olLmmVjEg+LiuKo7NVmmQ==
      </license>
    </licenses>
  </resizer>

  <system.web>
    <httpModules>
      <!-- This is for IIS5, IIS6, and IIS7 Classic, and Cassini/VS Web Server-->
      <add name="ImageResizingModule" type="ImageResizer.InterceptModule"/>
    </httpModules>
  </system.web>

  <system.webServer>
    <validation validateIntegratedModeConfiguration="false"/>
    <modules>
      <!-- This is for IIS7+ Integrated mode -->
      <add name="ImageResizingModule" type="ImageResizer.InterceptModule"/>
    </modules>
  </system.webServer>
</configuration>
```

### Can I just delete the code that enforces license keys?

Sure. None of our licenses prohibit removal of the DRM. Removing the DRM **doesn't free you from copyright law and the license terms (whether AGPL or commercial)**, it just means you won't be reminded to comply with them. It's also our tricky way to get you hooked on editing the source code (and, hopefully, sending us pull requests on GitHub).
