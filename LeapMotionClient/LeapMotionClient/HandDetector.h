#pragma once

#include "stdafx.h"
#include <thread>
#include "opencv2\core.hpp"
#include "opencv2\highgui.hpp"
#include "opencv2\imgproc\imgproc.hpp"

using namespace std;
using namespace cv;
using namespace concurrency;

#define RESULT_FILE_NAME_BASE	"calibration_data"
#define AVERAGING_KERNEL_SIZE	15

class HandDetector
{
public:

	HandDetector() {}

	Mat DetectHands(Mat* target_image, bool do_filtering)
	{
		Mat target = *target_image;

		// Read the result of skin sample training
		string line;
		ifstream result_file;
		result_file.open(MakeTrainingFileName());
		getline(result_file, line);
		result_file.close();

		// Read each object into its own string
		string sample_dim_str, cluster_count_str;
		stringstream ss(line);
		getline(ss, sample_dim_str, ';');
		getline(ss, cluster_count_str, ';');
		int sample_dim = stoi(sample_dim_str);
		int cluster_count = stoi(cluster_count_str);

		// Read each cluster
		vector<double> mah_lower_thresholds, mah_upper_thresholds;
		vector<Mat> means;
		vector<Mat> inv_covars;

		for (int i = 0; i < cluster_count; ++i)
		{
			string mah_mean_str, mah_std_dev_str, mean_str, inv_covar_str;
			getline(ss, mah_mean_str, ';');
			getline(ss, mah_std_dev_str, ';');
			getline(ss, mean_str, ';');
			getline(ss, inv_covar_str, ';');

			mah_lower_thresholds.push_back(0.0);
			mah_upper_thresholds.push_back(stod(mah_mean_str) + mah_std_dev_margin * stod(mah_std_dev_str));

			Mat mean(Size(sample_dim, 1), CV_64F);
			stringstream mean_ss(mean_str);
			for (int j = 0; j < sample_dim; ++j)
			{
				string comp_mean_str;
				getline(mean_ss, comp_mean_str, ',');
				double comp_mean = stod(comp_mean_str);
				mean.at<double>(0, j) = comp_mean;
			}
			means.push_back(mean);

			Mat inv_covar(Size(sample_dim, sample_dim), CV_64F);
			stringstream inv_covar_ss(inv_covar_str);
			for (int row = 0; row < sample_dim; ++row)
			{
				for (int col = 0; col < sample_dim; ++col)
				{
					string entry_str;
					getline(inv_covar_ss, entry_str, ',');
					double entry = stod(entry_str);
					inv_covar.at<double>(row, col) = entry;
				}
			}
			inv_covars.push_back(inv_covar);
		}

		// Convert the target image to the required color spaces
		Mat YCrCb, HSV, CIELab;
		Mat blurred_RGB, blurred_YCrCb, blurred_HSV, blurred_CIELab;

		GaussianBlur(target, target, Size(5, 5), 0.0);
		cvtColor(target, YCrCb, CV_BGR2YCrCb);
		cvtColor(target, HSV, CV_BGR2HSV);
		cvtColor(target, CIELab, CV_BGR2Lab);

		Mat surround_average_kernel = getStructuringElement(MORPH_ELLIPSE, Size(AVERAGING_KERNEL_SIZE, AVERAGING_KERNEL_SIZE));
		surround_average_kernel.at<uchar>(AVERAGING_KERNEL_SIZE / 2, AVERAGING_KERNEL_SIZE / 2) = 0;
		int kernel_sum = countNonZero(surround_average_kernel);
		surround_average_kernel.convertTo(surround_average_kernel, CV_32FC1);
		surround_average_kernel = surround_average_kernel / (float)kernel_sum;
		filter2D(target, blurred_RGB, -1, surround_average_kernel);
		cvtColor(blurred_RGB, blurred_YCrCb, CV_BGR2YCrCb);
		cvtColor(blurred_RGB, blurred_HSV, CV_BGR2HSV);
		cvtColor(blurred_RGB, blurred_CIELab, CV_BGR2Lab);

		target_rows = target.rows;
		target_cols = target.cols;

		// Calculate the Mahalanobis distance for each pixel to each cluster
		Mat initial_guess = Mat::zeros(target.size(), CV_8U);
		parallel_for(0, target_rows, [&](int row)
		{
			parallel_for(0, target_cols, [&](int col)
			{
				Vec3b rgb_pixel = target.at<Vec3b>(row, col);
				Vec3b ycrcb_pixel = YCrCb.at<Vec3b>(row, col);
				Vec3b hsv_pixel = HSV.at<Vec3b>(row, col);
				Vec3b cielab_pixel = CIELab.at<Vec3b>(row, col);
				Vec3b blurred_rgb_pixel = blurred_RGB.at<Vec3b>(row, col);
				Vec3b blurred_ycrcb_pixel = blurred_YCrCb.at<Vec3b>(row, col);
				Vec3b blurred_hsv_pixel = blurred_HSV.at<Vec3b>(row, col);
				Vec3b blurred_cielab_pixel = blurred_CIELab.at<Vec3b>(row, col);
				
				double rgb_sum = rgb_pixel[0] + rgb_pixel[1] + rgb_pixel[2];
				// Normalized RGB
				double nr, ng;
				if (rgb_sum > 0.0)
				{
					nr = ((double)rgb_pixel[2] / rgb_sum) * 255.0;
					ng = ((double)rgb_pixel[1] / rgb_sum) * 255.0;
				}
				else
				{
					nr = 0.0;
					ng = 0.0;
				}
				// Opponent colors
				int RG = rgb_pixel[2] - rgb_pixel[1];
				int YB = (2 * rgb_pixel[0] - rgb_pixel[2] + rgb_pixel[1]) / 4;
				
				double blurred_rgb_sum = blurred_rgb_pixel[0] + blurred_rgb_pixel[1] + blurred_rgb_pixel[2];
				// Blurred normalized RGB
				double blurred_nr, blurred_ng;
				if (blurred_rgb_sum > 0.0)
				{
					blurred_nr = ((double)blurred_rgb_pixel[2] / blurred_rgb_sum) * 255.0;
					blurred_ng = ((double)blurred_rgb_pixel[1] / blurred_rgb_sum) * 255.0;
				}
				else
				{
					blurred_nr = 0.0;
					blurred_ng = 0.0;
				}
				// Blurred opponent colors
				int blurred_RG = blurred_rgb_pixel[2] - blurred_rgb_pixel[1];
				int blurred_YB = (2 * blurred_rgb_pixel[0] - blurred_rgb_pixel[2] + blurred_rgb_pixel[1]) / 4;

				int target_col = 0;
				Mat transformed_pixel(Size(sample_dim, 1), CV_64F);
				// RGB
				transformed_pixel.at<double>(0, target_col++) = rgb_pixel[2];
				transformed_pixel.at<double>(0, target_col++) = rgb_pixel[1];
				transformed_pixel.at<double>(0, target_col++) = rgb_pixel[0];
				// Normalized RGB
				transformed_pixel.at<double>(0, target_col++) = nr;
				transformed_pixel.at<double>(0, target_col++) = ng;
				// Opponent colors
				transformed_pixel.at<double>(0, target_col++) = RG;
				transformed_pixel.at<double>(0, target_col++) = YB;
				// YCrCb
				transformed_pixel.at<double>(0, target_col++) = (double)ycrcb_pixel[0];
				transformed_pixel.at<double>(0, target_col++) = (double)ycrcb_pixel[1];
				transformed_pixel.at<double>(0, target_col++) = (double)ycrcb_pixel[2];
				// HSV
				transformed_pixel.at<double>(0, target_col++) = (double)hsv_pixel[0];
				transformed_pixel.at<double>(0, target_col++) = (double)hsv_pixel[1];
				transformed_pixel.at<double>(0, target_col++) = (double)hsv_pixel[2];
				// CIELab
				transformed_pixel.at<double>(0, target_col++) = (double)cielab_pixel[0];
				transformed_pixel.at<double>(0, target_col++) = (double)cielab_pixel[1];
				transformed_pixel.at<double>(0, target_col++) = (double)cielab_pixel[2];
				// Blurred RGB
				transformed_pixel.at<double>(0, target_col++) = blurred_rgb_pixel[2];
				transformed_pixel.at<double>(0, target_col++) = blurred_rgb_pixel[1];
				transformed_pixel.at<double>(0, target_col++) = blurred_rgb_pixel[0];
				// Blurred normalized RGB
				transformed_pixel.at<double>(0, target_col++) = blurred_nr;
				transformed_pixel.at<double>(0, target_col++) = blurred_ng;
				// Blurred opponent colors
				transformed_pixel.at<double>(0, target_col++) = blurred_RG;
				transformed_pixel.at<double>(0, target_col++) = blurred_YB;
				// Blurred YCrCb
				transformed_pixel.at<double>(0, target_col++) = (double)blurred_ycrcb_pixel[0];
				transformed_pixel.at<double>(0, target_col++) = (double)blurred_ycrcb_pixel[1];
				transformed_pixel.at<double>(0, target_col++) = (double)blurred_ycrcb_pixel[2];
				// Blurred HSV
				transformed_pixel.at<double>(0, target_col++) = (double)blurred_hsv_pixel[0];
				transformed_pixel.at<double>(0, target_col++) = (double)blurred_hsv_pixel[1];
				transformed_pixel.at<double>(0, target_col++) = (double)blurred_hsv_pixel[2];
				// Blurred CIELab
				transformed_pixel.at<double>(0, target_col++) = (double)blurred_cielab_pixel[0];
				transformed_pixel.at<double>(0, target_col++) = (double)blurred_cielab_pixel[1];
				transformed_pixel.at<double>(0, target_col++) = (double)blurred_cielab_pixel[2];

				for (int i = 0; i < cluster_count && initial_guess.at<uchar>(row, col) != 255; ++i)
				{
					double mah_distance = Mahalanobis(transformed_pixel, means[i], inv_covars[i]);
					if (mah_distance >= mah_lower_thresholds[i] && mah_distance <= mah_upper_thresholds[i])
					{
						initial_guess.at<uchar>(row, col) = 255;
					}
				}
			});
		});

		if (!do_filtering) return initial_guess;

		Mat filtered_image(initial_guess.size(), CV_8U);
		int corner_offset = 4;
		parallel_for (0, target_rows, [&](int row)
		{
			parallel_for (0, target_cols, [&](int col)
			{
				// Calculate corner coordinates to use
				// Corner 1
				int c1row = max(row - corner_offset, 0);
				int c1col = max(col - corner_offset, 0);
				// Corner 2
				int c2row = max(row - corner_offset, 0);
				int c2col = min(col + corner_offset, target_cols - 1);
				// Corner 3
				int c3row = min(row + corner_offset, target_rows - 1);
				int c3col = min(col + corner_offset, target_cols - 1);
				// Corner 4
				int c4row = min(row + corner_offset, target_rows - 1);
				int c4col = max(col - corner_offset, 0);

				// If all values are the same, we leave the pixel's value.
				// Otherwise we check all the pixels in the area to determine the value
				if (initial_guess.at<uchar>(row, col) != initial_guess.at<uchar>(c1row, c1col) ||
					initial_guess.at<uchar>(row, col) != initial_guess.at<uchar>(c2row, c2col) ||
					initial_guess.at<uchar>(row, col) != initial_guess.at<uchar>(c3row, c3col) ||
					initial_guess.at<uchar>(row, col) != initial_guess.at<uchar>(c4row, c4col))
				{
					int total_area = (c3row - c1row + 1) * (c2col - c1col + 1);
					int amount_under_threshold = 0;
					for (int i = c1row; i <= c3row; ++i)
					{
						for (int j = c1col; j <= c2col; ++j)
						{
							if (initial_guess.at<uchar>(i, j))
							{
								++amount_under_threshold;
							}
						}
					}
					float ratio = (float)amount_under_threshold / (float)total_area;
					filtered_image.at<uchar>(row, col) = ratio > area_threshold ? 255 : 0;
				}
				else
				{
					filtered_image.at<uchar>(row, col) = initial_guess.at<uchar>(row, col);
				}
			});
		});

		Mat closing_kernel = Mat::ones(Size(11, 11), CV_8U);
		morphologyEx(filtered_image, filtered_image, MORPH_CLOSE, closing_kernel, Point(-1, -1), 1);

		// Find all the connected areas in the image
		Mat labels;
		Mat stats;
		Mat centroids;

		int number_of_labels = connectedComponentsWithStats(filtered_image, labels, stats, centroids);

		// Find the two largest areas in the image
		int largest_label = 0;
		int largest_area = 0;
		int second_largest_label = 0;
		int second_largest_area = 0;
		for (int label = 1; label < number_of_labels; ++label)
		{
			int label_area = stats.at<int>(label, CC_STAT_AREA);
			if (label_area > largest_area)
			{
				second_largest_label = largest_label;
				second_largest_area = largest_area;
				largest_label = label;
				largest_area = label_area;
			}
			else if (label_area > second_largest_area)
			{
				second_largest_label = label;
				second_largest_area = label_area;
			}
		}

		for (int row = 0; row < target_rows; ++row)
		{
			for (int col = 0; col < target_cols; ++col)
			{
				int label = labels.at<int>(row, col);
				bool keep_pixel = label == largest_label || label == second_largest_label;
				if (!keep_pixel) filtered_image.at<uchar>(row, col) = 0;
			}
		}

		// Draw the areas as filled contours
		vector<vector<Point> > contours;
		vector<Vec4i> hierarchy;
		findContours(filtered_image, contours, hierarchy, RETR_EXTERNAL, CHAIN_APPROX_NONE, Point(0, 0));
		Mat result = Mat::zeros(initial_guess.size(), CV_8U);
		for (size_t i = 0; i< contours.size(); i++)
		{
			Scalar color = Scalar(255);
			drawContours(result, contours, (int)i, color, -1, 8, hierarchy, 0, Point());
		}

		blur(result, result, Size(11, 11));
		threshold(result, result, 255.0 * 0.6, 255.0, THRESH_BINARY);

		Mat dilation_kernel = Mat::ones(Size(9, 9), CV_8U);
		morphologyEx(result, result, MORPH_DILATE, dilation_kernel, Point(-1, -1), 1);

		return result;
	}

private:

	int				target_rows			= 0;
	int				target_cols			= 0;

	const double		max_rgb_sum			= 765.0;

	const double		mah_std_dev_margin	= 0.6;

	const float		area_threshold		= 0.6f;

	string MakeTrainingFileName()
	{
		return string(RESULT_FILE_NAME_BASE) + ".txt";
	}
};
