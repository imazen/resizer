

var grayscale = {
above:"### Grayscale modes", 
image:"red-leaf.jpg?width=100",
xfield:"s.grayscale",
xvalues:[
{alt:"NTSC/Y grayscale standard", v:"y"},
{alt:"R-Y Grayscale standard",  v:"ry"},
{alt:"BT709 (htdv) Grayscale standard",  v:"bt709"},
{alt:"Flat (50/50/50) Grayscale", v:"flat"},
{alt:"Saturation=-1", v:{"s.saturation=-1"}}]};

var sepia = {
above: "### Sepia",
image:"red-leaf.jpg?width=100",
xfield:["s.sepia","a.sepia"],


## SimpleFilters tests

![Invert](red-leaf.jpg?width=100&s.invert=true)

### Grayscale modes
![NTSC/Y grayscale standard](red-leaf.jpg?width=100&s.grayscale=y)
![R-Y Grayscale standard](red-leaf.jpg?width=100&s.grayscale=ry)
![BT709 (htdv) Grayscale standard](red-leaf.jpg?width=100&s.grayscale=bt709)
![Flat (50/50/50) Grayscale](red-leaf.jpg?width=100&s.grayscale=flat)
![Saturation=-1](red-leaf.jpg?width=100&s.saturation=-1)

### Sepia

![](red-leaf.jpg?width=100&s.sepia=true)
![](red-leaf.jpg?width=100&s.sepia=true&s.grayscale=true)
![](red-leaf.jpg?width=100&s.sepia=true&s.contrast=0.5)

![](red-leaf.jpg?width=100&a.sepia=true)
![](red-leaf.jpg?width=100&a.sepia=true&a.saturation=-1)
![](red-leaf.jpg?width=100&s.sepia=true&a.contrast=0.5)


### Saturation

![](red-leaf.jpg?width=100&s.saturation=-5)
![](red-leaf.jpg?width=100&s.saturation=-1)
![](red-leaf.jpg?width=100&s.saturation=-0.9)
![](red-leaf.jpg?width=100&s.saturation=-0.5)
![](red-leaf.jpg?width=100&s.saturation=-0.2)
![](red-leaf.jpg?width=100&s.saturation=0)
![](red-leaf.jpg?width=100&s.saturation=0.2)
![](red-leaf.jpg?width=100&s.saturation=0.5)
![](red-leaf.jpg?width=100&s.saturation=0.9)
![](red-leaf.jpg?width=100&s.saturation=1)
![](red-leaf.jpg?width=100&s.saturation=5)
![](red-leaf.jpg?width=100&s.saturation=10)

![](red-leaf.jpg?width=100&a.saturation=-5)
![](red-leaf.jpg?width=100&a.saturation=-1)
![](red-leaf.jpg?width=100&a.saturation=-0.9)
![](red-leaf.jpg?width=100&a.saturation=-0.5)
![](red-leaf.jpg?width=100&a.saturation=-0.2)
![](red-leaf.jpg?width=100&a.saturation=0)
![](red-leaf.jpg?width=100&a.saturation=0.2)
![](red-leaf.jpg?width=100&a.saturation=0.5)
![](red-leaf.jpg?width=100&a.saturation=0.9)
![](red-leaf.jpg?width=100&a.saturation=1)
![](red-leaf.jpg?width=100&a.saturation=5)
![](red-leaf.jpg?width=100&a.saturation=10)


![](red-leaf.jpg?width=100&a.saturation=-5&a.truncate=true)
![](red-leaf.jpg?width=100&a.saturation=-1&a.truncate=true)
![](red-leaf.jpg?width=100&a.saturation=-0.9&a.truncate=true)
![](red-leaf.jpg?width=100&a.saturation=-0.5&a.truncate=true)
![](red-leaf.jpg?width=100&a.saturation=-0.2&a.truncate=true)
![](red-leaf.jpg?width=100&a.saturation=0&a.truncate=true)
![](red-leaf.jpg?width=100&a.saturation=0.2&a.truncate=true)
![](red-leaf.jpg?width=100&a.saturation=0.5&a.truncate=true)
![](red-leaf.jpg?width=100&a.saturation=0.9&a.truncate=true)
![](red-leaf.jpg?width=100&a.saturation=1&a.truncate=true)
![](red-leaf.jpg?width=100&a.saturation=5&a.truncate=true)
![](red-leaf.jpg?width=100&a.saturation=10&a.truncate=true)


### Contrast

![](red-leaf.jpg?width=100&s.contrast=-5)
![](red-leaf.jpg?width=100&s.contrast=-1)
![](red-leaf.jpg?width=100&s.contrast=-0.9)
![](red-leaf.jpg?width=100&s.contrast=-0.5)
![](red-leaf.jpg?width=100&s.contrast=-0.2)
![](red-leaf.jpg?width=100&s.contrast=0)
![](red-leaf.jpg?width=100&s.contrast=0.2)
![](red-leaf.jpg?width=100&s.contrast=0.5)
![](red-leaf.jpg?width=100&s.contrast=0.9)
![](red-leaf.jpg?width=100&s.contrast=1)
![](red-leaf.jpg?width=100&s.contrast=5)
![](red-leaf.jpg?width=100&s.contrast=10)

![](red-leaf.jpg?width=100&a.contrast=-5)
![](red-leaf.jpg?width=100&a.contrast=-1)
![](red-leaf.jpg?width=100&a.contrast=-0.9)
![](red-leaf.jpg?width=100&a.contrast=-0.5)
![](red-leaf.jpg?width=100&a.contrast=-0.2)
![](red-leaf.jpg?width=100&a.contrast=0)
![](red-leaf.jpg?width=100&a.contrast=0.2)
![](red-leaf.jpg?width=100&a.contrast=0.5)
![](red-leaf.jpg?width=100&a.contrast=0.9)
![](red-leaf.jpg?width=100&a.contrast=1)
![](red-leaf.jpg?width=100&a.contrast=5)
![](red-leaf.jpg?width=100&a.contrast=10)


![](red-leaf.jpg?width=100&a.contrast=-5&a.truncate=true)
![](red-leaf.jpg?width=100&a.contrast=-1&a.truncate=true)
![](red-leaf.jpg?width=100&a.contrast=-0.9&a.truncate=true)
![](red-leaf.jpg?width=100&a.contrast=-0.5&a.truncate=true)
![](red-leaf.jpg?width=100&a.contrast=-0.2&a.truncate=true)
![](red-leaf.jpg?width=100&a.contrast=0&a.truncate=true)
![](red-leaf.jpg?width=100&a.contrast=0.2&a.truncate=true)
![](red-leaf.jpg?width=100&a.contrast=0.5&a.truncate=true)
![](red-leaf.jpg?width=100&a.contrast=0.9&a.truncate=true)
![](red-leaf.jpg?width=100&a.contrast=1&a.truncate=true)
![](red-leaf.jpg?width=100&a.contrast=5&a.truncate=true)
![](red-leaf.jpg?width=100&a.contrast=10&a.truncate=true)


### Brightness


![](red-leaf.jpg?width=100&s.brightness=-1)
![](red-leaf.jpg?width=100&s.brightness=-0.7)
![](red-leaf.jpg?width=100&s.brightness=-0.5)
![](red-leaf.jpg?width=100&s.brightness=-0.2)
![](red-leaf.jpg?width=100&s.brightness=0)
![](red-leaf.jpg?width=100&s.brightness=0.2)
![](red-leaf.jpg?width=100&s.brightness=0.5)
![](red-leaf.jpg?width=100&s.brightness=0.7)
![](red-leaf.jpg?width=100&s.brightness=1)


![](red-leaf.jpg?width=100&a.brightness=-1)
![](red-leaf.jpg?width=100&a.brightness=-0.7)
![](red-leaf.jpg?width=100&a.brightness=-0.5)
![](red-leaf.jpg?width=100&a.brightness=-0.2)
![](red-leaf.jpg?width=100&a.brightness=0)
![](red-leaf.jpg?width=100&a.brightness=0.2)
![](red-leaf.jpg?width=100&a.brightness=0.5)
![](red-leaf.jpg?width=100&a.brightness=0.7)
![](red-leaf.jpg?width=100&a.brightness=1)


![](red-leaf.jpg?width=100&a.brightness=-1&a.truncate=true)
![](red-leaf.jpg?width=100&a.brightness=-0.7&a.truncate=true)
![](red-leaf.jpg?width=100&a.brightness=-0.5&a.truncate=true)
![](red-leaf.jpg?width=100&a.brightness=-0.2&a.truncate=true)
![](red-leaf.jpg?width=100&a.brightness=0&a.truncate=true)
![](red-leaf.jpg?width=100&a.brightness=0.2&a.truncate=true)
![](red-leaf.jpg?width=100&a.brightness=0.5&a.truncate=true)
![](red-leaf.jpg?width=100&a.brightness=0.7&a.truncate=true)
![](red-leaf.jpg?width=100&a.brightness=1&a.truncate=true)
### Mixing 


![](red-leaf.jpg?width=100&s.brightness=-1&s.contrast=.7&s.saturation=0.5)
![](red-leaf.jpg?width=100&s.brightness=-0.7&s.contrast=.7&s.saturation=0.5)
![](red-leaf.jpg?width=100&s.brightness=-0.5&s.contrast=.7&s.saturation=0.5)
![](red-leaf.jpg?width=100&s.brightness=-0.2&s.contrast=.7&s.saturation=0.5)
![](red-leaf.jpg?width=100&s.brightness=0&s.contrast=.7&s.saturation=0.5)
![](red-leaf.jpg?width=100&s.brightness=0.2&s.contrast=.7&s.saturation=0.5)
![](red-leaf.jpg?width=100&s.brightness=0.5&s.contrast=.7&s.saturation=0.5)
![](red-leaf.jpg?width=100&s.brightness=0.7&s.contrast=.7&s.saturation=0.5)
![](red-leaf.jpg?width=100&s.brightness=1&s.contrast=.7&s.saturation=0.5)

![](red-leaf.jpg?width=100&a.brightness=-1&a.contrast=.7&a.saturation=0.5)
![](red-leaf.jpg?width=100&a.brightness=-0.7&a.contrast=.7&a.saturation=0.5)
![](red-leaf.jpg?width=100&a.brightness=-0.5&a.contrast=.7&a.saturation=0.5)
![](red-leaf.jpg?width=100&a.brightness=-0.2&a.contrast=.7&a.saturation=0.5)
![](red-leaf.jpg?width=100&a.brightness=0&a.contrast=.7&a.saturation=0.5)
![](red-leaf.jpg?width=100&a.brightness=0.2&a.contrast=.7&a.saturation=0.5)
![](red-leaf.jpg?width=100&a.brightness=0.5&a.contrast=.7&a.saturation=0.5)
![](red-leaf.jpg?width=100&a.brightness=0.7&a.contrast=.7&a.saturation=0.5)
![](red-leaf.jpg?width=100&a.brightness=1&a.contrast=.7&a.saturation=0.5)



### Alpha adjustment


![](red-leaf.jpg?width=100&s.alpha=0.1)
![](red-leaf.jpg?width=100&s.alpha=0.5)
![](red-leaf.jpg?width=100&s.alpha=0.9)

