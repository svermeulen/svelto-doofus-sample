
Svelto Doofus Sample
--------

This is a re-write of the [svelto doofus example](https://github.com/sebas77/Svelto.MiniExamples).  Main difference is that engines are run in parallel, and it uses GPU Instancer for rendering, to further stress-test overall svelto performance.  This allows running with 500k entities at 60fps on a decent GPU.

However - since GPU Instancer is a paid asset, this has been ommitted from this repo.  So in order to run this sample you will need to purchase it [here](https://assetstore.unity.com/packages/tools/utilities/gpu-instancer-117566) and then install it to Assets/GPUInstancer

Once working you can modify the amount of entities by changing NumDoofusesPerTeam in DoofusConstants.cs

