#pragma once
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
using namespace cv;
class Depth_Colorizer
{
private:
public:
	Mat depth16, dst;
	Depth_Colorizer() { }
	void Run()
	{
		unsigned char nearColor[3] = { 0, 255, 255 };
		unsigned char farColor[3] = { 255, 0, 0 };
		int histogram[256 * 256] = { 1 };
		// Produce a cumulative histogram of depth values
		unsigned short* depthImage = (unsigned short*)depth16.data;
		for (int i = 0; i < depth16.cols * depth16.rows; ++i)
		{
			if (auto d = depthImage[i]) ++histogram[d];
		}
		for (int i = 1; i < 256 * 256; i++)
		{
			histogram[i] += histogram[i - 1];
		}

		// Remap the cumulative histogram to the range 0..256
		for (int i = 1; i < 256 * 256; i++)
		{
			histogram[i] = (histogram[i] << 8) / histogram[256 * 256 - 1];
		}

		// Produce RGB image by using the histogram to interpolate between two colors
		auto rgb = (unsigned char*)dst.data;
		for (int i = 0; i < dst.cols * dst.rows; i++)
		{
			if (uint16_t d = depthImage[i]) // For valid depth values (depth > 0)
			{
				auto t = histogram[d]; // Use the histogram entry (in the range of 0..256) to interpolate between nearColor and farColor
				*rgb++ = ((256 - t) * nearColor[0] + t * farColor[0]) >> 8;
				*rgb++ = ((256 - t) * nearColor[1] + t * farColor[1]) >> 8;
				*rgb++ = ((256 - t) * nearColor[2] + t * farColor[2]) >> 8;
			}
			else // Use black pixels for invalid values (depth == 0)
			{
				*rgb++ = 0;
				*rgb++ = 0;
				*rgb++ = 0;
			}
		}
	}
};





class Depth_Colorizer32f
{
private:
public:
	Mat depth32f, dst;
	Depth_Colorizer32f() {  }
	void Run()
	{
		unsigned char nearColor[3] = { 0, 255, 255 };
		unsigned char farColor[3] = { 255, 0, 0 };
		int histogram[256 * 256] = { 1 };
		// Produce a cumulative histogram of depth values
		float* depthImage = (float*)depth32f.data;
		for (int i = 0; i < depth32f.cols * depth32f.rows; ++i)
		{
			if (auto d = (int)depthImage[i]) ++histogram[d];
		}
		for (int i = 1; i < 256 * 256; i++)
		{
			histogram[i] += histogram[i - 1];
		}

		// Remap the cumulative histogram to the range 0..256
		for (int i = 1; i < 256 * 256; i++)
		{
			histogram[i] = (histogram[i] << 8) / histogram[256 * 256 - 1];
		}

		// Produce RGB image by using the histogram to interpolate between two colors
		auto rgb = (unsigned char*)dst.data;
		for (int i = 0; i < dst.cols * dst.rows; i++)
		{
			if (int d = (int)depthImage[i]) // For valid depth values (depth > 0)
			{
				auto t = histogram[d]; // Use the histogram entry (in the range of 0..256) to interpolate between nearColor and farColor
				*rgb++ = ((256 - t) * nearColor[0] + t * farColor[0]) >> 8;
				*rgb++ = ((256 - t) * nearColor[1] + t * farColor[1]) >> 8;
				*rgb++ = ((256 - t) * nearColor[2] + t * farColor[2]) >> 8;
			}
			else // Use black pixels for invalid values (depth == 0)
			{
				*rgb++ = 0;
				*rgb++ = 0;
				*rgb++ = 0;
			}
		}
	}
};





class Depth_Colorizer2
{
private:
public:
	Mat depth16, dst;
	int histSize = 255;
	Depth_Colorizer2() {} 
	void Run()
	{
		float nearColor[3] = { 0, 1.0f, 1.0f };
		float farColor[3] = { 1.0f, 0, 0 };
		// Produce a cumulative histogram of depth values
		float hRange[] = { 1, float(histSize) }; // ranges are exclusive at the top of the range
		const float* range[] = { hRange };
		int hbins[] = { histSize };
		Mat hist;
		if (countNonZero(depth16) > 0)
		{
			calcHist(&depth16, 1, 0, Mat(), hist, 1, hbins, range, true, false);
		}
		else {
			dst.setTo(0);
			return; // there is nothing to measure so just return zeros.
		}

		float* histogram = (float*)hist.data;
		for (int i = 1; i < histSize; i++)
		{
			histogram[i] += histogram[i - 1];
		}

		if (histogram[histSize - 1] > 0)
		{
			hist *= 1.0f / histogram[histSize - 1];

			// Produce RGB image by using the histogram to interpolate between two colors
			auto rgb = (unsigned char*)dst.data;
			unsigned short* depthImage = (unsigned short*)depth16.data;
			for (int i = 0; i < dst.cols * dst.rows; i++)
			{
				if (uint16_t d = depthImage[i]) // For valid depth values (depth > 0)
				{
					if (d < histSize)
					{
						auto t = histogram[d]; // Use the histogram entry (in the range of 0..1) to interpolate between nearColor and farColor
						*rgb++ = uchar(((1 - t) * nearColor[0] + t * farColor[0]) * 255);
						*rgb++ = uchar(((1 - t) * nearColor[1] + t * farColor[1]) * 255);
						*rgb++ = uchar(((1 - t) * nearColor[2] + t * farColor[2]) * 255);
					}
					else {
						*rgb++ = 0; *rgb++ = 0; *rgb++ = 0;
					}
				}
				else // Use black pixels for invalid values (depth16 == 0)
				{
					*rgb++ = 0; *rgb++ = 0; *rgb++ = 0;
				}
			}
		}
	}
};
