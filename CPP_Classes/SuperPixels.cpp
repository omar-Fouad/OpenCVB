#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/ximgproc.hpp>

using namespace std;
using namespace cv;
class SuperPixels
{
private:
public:
	Mat src, labels, dst;
	Ptr<cv::ximgproc::SuperpixelSEEDS> seeds;
	int width, height, num_superpixels = 400, num_levels = 4, prior = 2;
	SuperPixels(){}
    void Run() 
	{
		Mat hsv;
		//cvtColor(src, hsv, cv::ColorConversionCodes::COLOR_BGR2HSV);
		seeds->iterate(src);
		seeds->getLabelContourMask(dst, false);
		seeds->getLabels(labels);
    }
};

extern "C" __declspec(dllexport)
SuperPixels *SuperPixel_Open(int _width, int _height, int _num_superpixels, int _num_levels, int _prior) 
{
    SuperPixels *spPtr = new SuperPixels();
	spPtr->width = _width;
	spPtr->height = _height;
	spPtr->num_superpixels = _num_superpixels;
	spPtr->num_levels = _num_levels;
	spPtr->prior = _prior;
	spPtr->seeds = cv::ximgproc::createSuperpixelSEEDS(_width, _height, 3, _num_superpixels, _num_levels, _prior);
	spPtr->labels = Mat(spPtr->height, spPtr->width, CV_32S);
	return spPtr;
}

extern "C" __declspec(dllexport)
void SuperPixel_Close(SuperPixels * spPtr)
{
	delete spPtr;
}

extern "C" __declspec(dllexport)
int *SuperPixel_GetLabels(SuperPixels * spPtr)
{
	return (int *) spPtr->labels.data;
}

extern "C" __declspec(dllexport)
int *SuperPixel_Run(SuperPixels *spPtr, int* srcPtr)
{
	spPtr->src = Mat(spPtr->height, spPtr->width, CV_8UC3, srcPtr);
	spPtr->Run();
	return (int *) spPtr->dst.data; // return this C++ allocated data to managed code
}