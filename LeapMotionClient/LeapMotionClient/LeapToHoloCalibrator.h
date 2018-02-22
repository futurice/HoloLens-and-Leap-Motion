#pragma once

#include "Leap.h"
#include "stdafx.h"
#include "opencv2\core.hpp"
#include "opencv2\calib3d.hpp"

using namespace std;
using namespace cv;
using namespace Leap;

class LeapToHoloCalibrator
{
public:

	LeapToHoloCalibrator() {}

	void Calibrate(Mat* rot_mat, Mat* trans_vec, float fx, float fy, float cx, float cy, vector<Point2f>* image_fingertips, vector<Point3f>* leap_fingertips)
	{
		// Build the calibration matrix
		Mat calib_matrix = Mat::zeros(Size(3, 3), CV_64F);
		// Values from Hololens
		/*calib_matrix.at<double>(0, 0) = (double)fx;
		calib_matrix.at<double>(1, 1) = (double)fy;
		calib_matrix.at<double>(0, 2) = (double)cx;
		calib_matrix.at<double>(1, 2) = (double)cy;
		calib_matrix.at<double>(2, 2) = (double)1.0f;*/

		// Values from manual calibration
		calib_matrix.at<double>(0, 0) = 1605.164063;
		calib_matrix.at<double>(1, 1) = 1604.750732;
		calib_matrix.at<double>(0, 2) = 1023.521851;
		calib_matrix.at<double>(1, 2) = 543.316895;
		calib_matrix.at<double>(2, 2) = (double)1.0f;

		Mat distortion = Mat::zeros(4, 1, CV_64F);
		distortion.at<double>(0, 0) = 0.153665;
		distortion.at<double>(1, 0) = 0.107066;
		distortion.at<double>(2, 0) = -0.008653;
		distortion.at<double>(3, 0) = -0.000786;

		// First do once using EPNP to create a starting point for iteration
		Mat rotation_vector, translation_vector;
		solvePnP(*leap_fingertips, *image_fingertips, calib_matrix, distortion, rotation_vector, translation_vector, false, SOLVEPNP_EPNP);
		// Refine using iteration
		solvePnP(*leap_fingertips, *image_fingertips, calib_matrix, distortion, rotation_vector, translation_vector, true, SOLVEPNP_ITERATIVE);

		rotation_vector.at<double>(0, 0) += 0.088;
		rotation_vector.at<double>(0, 1) += 0.015;
		translation_vector.at<double>(2, 0) += 0.045;

		Mat rotation_matrix;
		Rodrigues(rotation_vector, rotation_matrix);

		// Write results
		for (int row = 0; row < 3; ++row)
		{
			for (int col = 0; col < 3; ++col)
			{
				rot_mat->at<double>(row, col) = rotation_matrix.at<double>(row, col);
			}
		}
		trans_vec->at<double>(0, 0) = translation_vector.at<double>(0, 0);
		trans_vec->at<double>(1, 0) = translation_vector.at<double>(1, 0);
		trans_vec->at<double>(2, 0) = translation_vector.at<double>(2, 0);
	}

private:
};