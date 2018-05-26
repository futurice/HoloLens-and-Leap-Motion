#pragma once

#include "stdafx.h"
#include "opencv2\core.hpp"
#include "opencv2\highgui.hpp"
#include "opencv2\imgproc\imgproc.hpp"
#include <list>

using namespace std;
using namespace cv;
using namespace concurrency;

class FingertipDetector
{
public:

	FingertipDetector() 
	{
		opening_kernel = getStructuringElement(MORPH_ELLIPSE, Size(opening_kernel_size, opening_kernel_size));
	}

	/// Find all fingertips in an image containing two hands and returns them ordered left to right
	void FindFingertips(Mat* source_image, vector<Point2f>* fingertips)
	{
		// Separate each hand into separate images
		Mat h1, h2;
		Mat hand_labels;
		Mat stats;
		Mat centroids;
		
		int label_amt = connectedComponentsWithStats(*source_image, hand_labels, stats, centroids);
		// Background always has label 0, so we get the hands from labels 1 and 2
		inRange(hand_labels, 1, 1, h1);
		inRange(hand_labels, 2, 2, h2);

		// Find all fingertips for each hand and then sort them left to right
		vector<Point2f> tips;
		AnalyzeHand(&h1, &tips);
		AnalyzeHand(&h2, &tips);
		std::sort(tips.begin(), tips.end(), [](Point2f a, Point2f b)
		{
			return a.x < b.x;
		});

		// Copy all found fingertips to output
		int limit = tips.size();
		for (int i = 0; i < limit; ++i)
		{
			fingertips->push_back(tips[i]);
		}
	}

private:

	int const	opening_kernel_size = 81;
	Mat			opening_kernel;

	/// Analyzes an image with a single hand in it and extracts the fingertips from it
	void AnalyzeHand(Mat* hand_image, vector<Point2f>* fingertips)
	{
		// Open image and create top hat image
		Mat opened_image, top_hat_image;
		morphologyEx(*hand_image, opened_image, MORPH_OPEN, opening_kernel, Point(-1, -1), 2);
		top_hat_image = *hand_image - opened_image;
		//Find centroid
		Moments m = moments(opened_image, true);
		Point centroid(m.m10 / m.m00, m.m01 / m.m00);

		/*imwrite("./fingertips/hand.jpg", *hand_image);
		imwrite("./fingertips/opened.jpg", opened_image);
		Mat opened_colour;
		cvtColor(opened_image, opened_colour, COLOR_GRAY2BGR);
		Scalar green(0, 255, 0);
		circle(opened_colour, centroid, 20, green, -1);
		imwrite("./fingertips/centroid.jpg", opened_colour);
		imwrite("./fingertips/tophat.jpg", top_hat_image);*/

		// Find the 5 largest areas from top hat image. These should be the fingers
		Mat labels, stats, centroids;
		int label_amt = connectedComponentsWithStats(top_hat_image, labels, stats, centroids);
		list<int> ordered_labels, label_areas;
		// Sort the labels in descending order
		for (int label = 1; label < label_amt; ++label)
		{
			int label_area = stats.at<int>(label, CC_STAT_AREA);

			// If this is the first label, just add it to the lists
			if (ordered_labels.empty())
			{
				ordered_labels.push_back(label);
				label_areas.push_back(label_area);
			}
			else
			{
				// Iterate over the lists to find the correct position for the current label
				auto label_iter = ordered_labels.begin();
				auto area_iter = label_areas.begin();

				bool pos_found = false;
				while (label_iter != ordered_labels.end() && !pos_found)
				{
					pos_found = label_area > *area_iter;
					if (!pos_found)
					{
						++label_iter;
						++area_iter;
					}
				}

				if (label_iter == ordered_labels.end())
				{
					ordered_labels.push_back(label);
					label_areas.push_back(label_area);
				}
				else
				{
					ordered_labels.insert(label_iter, label);
					label_areas.insert(area_iter, label_area);
				}
			}
		}

		// Choose the 5 largest labels
		vector<int> fingers;
		auto iter = ordered_labels.begin();
		for (int i = 0; i < 5; ++i)
		{
			fingers.push_back(*iter);
			++iter;
		}

		/*Mat hand_colour;
		cvtColor(*hand_image, hand_colour, COLOR_GRAY2BGR);
		circle(hand_colour, centroid, 20, green, -1);
		Mat fingers_image = Mat::zeros(hand_colour.size(), CV_8U);*/

		// Find all fingertips and add them to output
		vector<Point2f> tips;
		int rows = labels.rows;
		int cols = labels.cols;
		for (int i = 0; i < fingers.size(); ++i)
		{
			int finger_label = fingers[i];
			Point2i furthest_point;
			double furthest_distance = -1.0;
			for (int row = 0; row < rows; ++row)
			{
				for (int col = 0; col < cols; ++col)
				{
					int current_label = labels.at<int>(row, col);
					if (current_label == finger_label)
					{
						//fingers_image.at<uchar>(row, col) = 225;
						if (furthest_distance < 0.0)
						{
							furthest_point = Point2i(col, row);
							furthest_distance = norm(furthest_point - centroid);
						}
						else
						{
							Point2i current_point(col, row);
							double distance = norm(current_point - centroid);
							if (distance > furthest_distance)
							{
								furthest_point = current_point;
								furthest_distance = distance;
							}
						}
					}
				}
			}
			fingertips->push_back((Point2f)furthest_point);
			/*Scalar red(0, 0, 255);
			circle(hand_colour, furthest_point, 20, red, -1);*/
		}

		/*imwrite("./fingertips/fingers.jpg", fingers_image);
		imwrite("./fingertips/fingertips.jpg", hand_colour);*/
	}
};