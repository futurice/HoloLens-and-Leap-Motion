# HoloLens with Leap Motion controller

This repository contains the solution developed for combining the HoloLens with a Leap Motion Controller (LMC). The work was done as a Master's Thesis work for [Aalto University](http://www.aalto.fi/en/) at [Futurice](https://futurice.com/) and the complete work detailing how everything works can be found [here](https://aaltodoc.aalto.fi/handle/123456789/29268). The purpose is to enable the creation of better hand and gesture based interactions for the HoloLens by making use of the hand tracking data provided by the LMC.

## Requirements

The Leap Motion client requires the [Orion SDK](https://developer.leapmotion.com/get-started/) and [OpenCV](https://opencv.org/) to run. The Skin Detection Trainer only requires OpenCV to run.

The Unity application uses [Zenject](https://github.com/modesttree/Zenject) for dependency injection and [UniRx](https://github.com/neuecc/UniRx) for Reactive extensions, but these are already included in the project.

## Solution details

The entire solution is made up of three separate applications (the Unity application running on the HoloLens, the Leap Motion client that provides the Leap Motion data, and the Skin Color Detection Trainer that calculates the parameters used for skin detection) and solves three key problems. These problems are:

1. Calibrating the HoloLens and the LMC. This is done with [Perspective-n-Point](https://en.wikipedia.org/wiki/Perspective-n-Point) and uses the user's fingertips in 1 or more images in combination with the 3D fingertip coordinates provided by the LMC. The implementation of the hand detection in an RGB image can be found [here](https://github.com/futurice/HoloLens-and-Leap-Motion/blob/master/LeapMotionClient/LeapMotionClient/HandDetector.h) and the fingertip detection can be found [here](https://github.com/futurice/HoloLens-and-Leap-Motion/blob/master/LeapMotionClient/LeapMotionClient/FingertipDetector.h).
2. Calculating the parameters needed for the hand detection. Skin colour detection is done using a combination of [k-means clustering](https://en.wikipedia.org/wiki/K-means_clustering) and [Mahalanobis distances](https://en.wikipedia.org/wiki/Mahalanobis_distance). The required paramaters are calculated based on a set of training images with corresponding ground truths. The trainer can be found [here](https://github.com/futurice/HoloLens-and-Leap-Motion/blob/master/SkinColorDetectionTrainer/SkinColorDetectionTrainer/SkinColorDetectionTrainer.cpp), the training set [here](https://github.com/futurice/HoloLens-and-Leap-Motion/tree/master/SkinColorDetectionTrainer/SkinColorDetectionTrainer/training_images), and the ground truths [here](https://github.com/futurice/HoloLens-and-Leap-Motion/tree/master/SkinColorDetectionTrainer/SkinColorDetectionTrainer/ground_truths).
3. Streaming and converting the Leap Motion data. Once calibration is done the data provided by the LMC are streamed using UDP to minimise latency. The data are converted on the HoloLens side to the HoloLens's coordinate system.

## Current limitations

The biggest current limitation is the skin colour detection. Since this project was done as a proof-of-concept, the training data for the skin colour detection was gathered only from a single person. If the hand detector doesn't seem to be working properly or accurately enough, this might be the cause. One option is then to collect additional training data using the HoloLens. But one problem that will probably still persist is that pure colour-based detection has problems with some colours (see chapters 4.1.5 and 5.1.1 of the thesis for more info). The optimal solution would be to change to a more reliable way of doing hand detection.

Another thing to note is that the results are not 100% accurate and further investigation of the source(s) of the error is needed. See chapter 5.1.2 of the thesis for a discussion of this error.

Third, the current networking code was written quite hastily, so any improvement suggestions to this are very welcomed. The HoloLens's UI is also very quickly thrown together, so any improvement suggestions to this are also very welcomed.

## Feedback, suggestions, and questions

Any feedback, suggestions for improvements, and questions about how something works are extremly welcome. Send any of these to nestor.kohler@futurice.com.

## License

This software is licensed under [Apache License 2.0](https://github.com/futurice/HoloLens-and-Leap-Motion/blob/master/LICENSE).

[UniRx](https://github.com/neuecc/UniRx) is licensed under MIT license, &copy; 2014 Yoshifumi Kawai.

[Zenject](https://github.com/modesttree/Zenject) is licensed under MIT license, &copy; 2016 Modest Tree Media Inc.