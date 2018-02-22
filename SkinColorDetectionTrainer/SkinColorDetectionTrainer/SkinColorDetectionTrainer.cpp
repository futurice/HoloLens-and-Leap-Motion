// SkinColorDetectionTrainer.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "Utils.h"

using namespace std;
using namespace cv;
using namespace ml;
using namespace concurrency;

#define TRAINING_IMAGES_FOLDER		"./training_images/"
#define GROUND_TRUTHS_FOLDER		"./ground_truths/"
#define RESULTS_FOLDER				"./training_results/"
#define RESULT_FILE_BASE_NAME		"training_result_"
#define BASE_SAMPLE_DIMENSION		16
#define CLUSTERING_ATTEMPTS			20
#define CLUSTERING_ITERATIONS		400
#define CLUSTERING_EPSILON			0.1

int		min_cluster_count = 1;
int		max_cluster_count = 10;
bool	use_surrounding_values = true;
int		averaging_kernel_size = 19;
bool	use_errosion = true;

float	max_rgb_sum		= 765.0f;
int		min_intensity	= 15;
int		max_intensity	= 250;

string MakeTrainingImageName(int current_number)
{
	return TRAINING_IMAGES_FOLDER + to_string(current_number) + string(".jpg");
}

string MakeGroundTruthName(int current_number)
{
	return GROUND_TRUTHS_FOLDER + to_string(current_number) + string(".jpg");
}

string MakeResultFileName(int cluster_amount)
{
	string name = string(RESULTS_FOLDER) + string(RESULT_FILE_BASE_NAME);
	if (use_surrounding_values)
	{
		name += string("use_surrounding_values_kernel") + to_string(averaging_kernel_size);

		if (use_errosion)
		{
			name += string("_use_erosion");
		}
		else
		{
			name += string("_no_erosion");
		}
	}
	else
	{
		name += string("no_surrounding_values");
	}
	name += "_clusters" + to_string(cluster_amount) + string(".txt");
	return name;
}

bool ReadImageFile(Mat* img, string file_name)
{
	*img = imread(file_name, IMREAD_UNCHANGED);
	return (*img).data;
}

size_t CountNumberOfSamples()
{
	atomic<size_t> sample_amount{ 0 };

	Mat current_ground_truth, current_image;
	int current_ground_truth_number = 1;

	while (ReadImageFile(&current_image, MakeTrainingImageName(current_ground_truth_number)))
	{
		ReadImageFile(&current_ground_truth, MakeGroundTruthName(current_ground_truth_number));
		cvtColor(current_ground_truth, current_ground_truth, CV_BGR2GRAY);
		threshold(current_ground_truth, current_ground_truth, 200, 255, THRESH_BINARY);
		Mat masked_data;
		bitwise_and(current_image, current_image, masked_data, current_ground_truth);
		GaussianBlur(masked_data, masked_data, Size(5, 5), 0.0);

		Mat kernel = getStructuringElement(MORPH_ELLIPSE, Size(averaging_kernel_size, averaging_kernel_size));
		if (use_errosion)
		{
			morphologyEx(current_ground_truth, current_ground_truth, MORPH_ERODE, kernel);
		}

		int rows = current_ground_truth.rows;
		int cols = current_ground_truth.cols;

		parallel_for(0, rows, [&](int row)
		{
			parallel_for(0, cols, [&](int col)
			{
				uchar ground_truth = current_ground_truth.at<uchar>(row, col);
				if (ground_truth == 255)
				{
					Vec3b current_pixel = masked_data.at<Vec3b>(row, col);
					int highest_intensity = max(current_pixel[0], max(current_pixel[1], current_pixel[2]));
					int lowest_intensity = min(current_pixel[0], min(current_pixel[1], current_pixel[2]));
					if (highest_intensity >= min_intensity && lowest_intensity <= max_intensity)
					{
						++sample_amount;
					}
				}
			});
		});

		++current_ground_truth_number;
	}

	return sample_amount.load();
}

void BuildSampleMatrix(Mat* sample_matrix)
{
	atomic<size_t> current_sample_index{ 0 };

	Mat YCrCb, HSV, CIELab;
	Mat blurred_RGB, blurred_YCrCb, blurred_HSV, blurred_CIELab;

	Mat current_learning_data, current_ground_truth;
	int current_test_data_number = 1;
	string training_image_name = MakeTrainingImageName(current_test_data_number);

	while (ReadImageFile(&current_learning_data, training_image_name))
	{
		// Load the ground truth and use it to mask the learning data
		ReadImageFile(&current_ground_truth, MakeGroundTruthName(current_test_data_number));
		cvtColor(current_ground_truth, current_ground_truth, CV_BGR2GRAY);
		threshold(current_ground_truth, current_ground_truth, 200, 255, THRESH_BINARY);
		Mat masked_data;
		bitwise_and(current_learning_data, current_learning_data, masked_data, current_ground_truth);
		Mat erode_kernel = getStructuringElement(MORPH_ELLIPSE, Size(averaging_kernel_size, averaging_kernel_size));
		if (use_errosion)
		{
			morphologyEx(current_ground_truth, current_ground_truth, MORPH_ERODE, erode_kernel);
		}

		// Blur masked image and convert to needed color spaces
		GaussianBlur(masked_data, masked_data, Size(5, 5), 0.0);
		cvtColor(masked_data, YCrCb, CV_BGR2YCrCb);
		cvtColor(masked_data, HSV, CV_BGR2HSV);
		cvtColor(masked_data, CIELab, CV_BGR2Lab);

		// Create blurred image that only takes into account the surrounding pixels
		Mat surround_average_kernel = getStructuringElement(MORPH_ELLIPSE, Size(averaging_kernel_size, averaging_kernel_size));
		surround_average_kernel.at<uchar>(averaging_kernel_size / 2, averaging_kernel_size / 2) = 0;
		int kernel_sum = countNonZero(surround_average_kernel);
		surround_average_kernel.convertTo(surround_average_kernel, CV_32FC1);
		surround_average_kernel = surround_average_kernel / (float)kernel_sum;
		filter2D(masked_data, blurred_RGB, -1, surround_average_kernel);
		cvtColor(blurred_RGB, blurred_YCrCb, CV_BGR2YCrCb);
		cvtColor(blurred_RGB, blurred_HSV, CV_BGR2HSV);
		cvtColor(blurred_RGB, blurred_CIELab, CV_BGR2Lab);

		// Loop over images and save values to samples array
		int rows = masked_data.rows;
		int cols = masked_data.cols;
		parallel_for (0, rows, [&](int row)
		{
			parallel_for(0, cols, [&](int col)
			{
				uchar ground_truth = current_ground_truth.at<uchar>(row, col);
				if (ground_truth == 255)
				{
					Vec3b current_pixel = masked_data.at<Vec3b>(row, col);
					int highest_intensity = max(current_pixel[0], max(current_pixel[1], current_pixel[2]));
					int lowest_intensity = min(current_pixel[0], min(current_pixel[1], current_pixel[2]));

					// Skip pixels that are too light or dark
					if (highest_intensity >= min_intensity && lowest_intensity <= max_intensity)
					{
						int sample_index = current_sample_index++;

						float rgb_sum = current_pixel[0] + current_pixel[1] + current_pixel[2];

						// Normalized red and green
						int nr = ((float)current_pixel[2] / rgb_sum) * 255.0f;
						int ng = ((float)current_pixel[1] / rgb_sum) * 255.0f;

						// Opponent colors
						int RG = current_pixel[2] - current_pixel[1];
						int YB = (2 * current_pixel[0] - current_pixel[2] + current_pixel[1]) / 4;

						// YCbCr
						Vec3b ycbcr_pixel = YCrCb.at<Vec3b>(row, col);

						// HSV
						Vec3b hsv_pixel = HSV.at<Vec3b>(row, col);

						// CIELab
						Vec3b cielab_pixel = CIELab.at<Vec3b>(row, col);

						// Blurred values
						Vec3b blurred_pixel = blurred_RGB.at<Vec3b>(row, col);
						float blurred_rgb_sum = blurred_pixel[0] + blurred_pixel[1] + blurred_pixel[2];

						// Blurred normalized red and green
						int blurred_nr = ((float)blurred_pixel[2] / blurred_rgb_sum) * 255.0f;
						int blurred_ng = ((float)blurred_pixel[1] / blurred_rgb_sum) * 255.0f;

						// Blurred opponent colors
						int blurred_RG = blurred_pixel[2] - blurred_pixel[1];
						int blurred_YB = (2 * blurred_pixel[0] - blurred_pixel[2] + blurred_pixel[1]) / 4;

						// Blurred YCbCr
						Vec3b blurred_ycbcr_pixel = blurred_YCrCb.at<Vec3b>(row, col);

						// Blurred HSV
						Vec3b blurred_hsv_pixel = blurred_HSV.at<Vec3b>(row, col);

						// Blurred CIELab
						Vec3b blurred_cielab_pixel = blurred_CIELab.at<Vec3b>(row, col);

						/*----------------------- Add sample values -----------------------------------------*/
						
						int target_col = 0;
						// RGB
						sample_matrix->at<uchar>(sample_index, target_col++) = current_pixel[2];
						sample_matrix->at<uchar>(sample_index, target_col++) = current_pixel[1];
						sample_matrix->at<uchar>(sample_index, target_col++) = current_pixel[0];

						// Normalized red and green
						sample_matrix->at<uchar>(sample_index, target_col++) = nr;
						sample_matrix->at<uchar>(sample_index, target_col++) = ng;

						// Opponent colors
						sample_matrix->at<uchar>(sample_index, target_col++) = RG;
						sample_matrix->at<uchar>(sample_index, target_col++) = YB;

						// YCbCr
						sample_matrix->at<uchar>(sample_index, target_col++) = ycbcr_pixel[0];
						sample_matrix->at<uchar>(sample_index, target_col++) = ycbcr_pixel[1];
						sample_matrix->at<uchar>(sample_index, target_col++) = ycbcr_pixel[2];

						// HSV
						sample_matrix->at<uchar>(sample_index, target_col++) = hsv_pixel[0];
						sample_matrix->at<uchar>(sample_index, target_col++) = hsv_pixel[1];
						sample_matrix->at<uchar>(sample_index, target_col++) = hsv_pixel[2];

						// CIELab
						sample_matrix->at<uchar>(sample_index, target_col++) = cielab_pixel[0];
						sample_matrix->at<uchar>(sample_index, target_col++) = cielab_pixel[1];
						sample_matrix->at<uchar>(sample_index, target_col++) = cielab_pixel[2];

						if (use_surrounding_values)
						{
							// Blurred RGB
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_pixel[2];
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_pixel[1];
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_pixel[0];

							// Blurred normalized red and green
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_nr;
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_ng;

							// Blurred opponent colors
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_RG;
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_YB;

							// Blurred YCbCr
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_ycbcr_pixel[0];
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_ycbcr_pixel[1];
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_ycbcr_pixel[2];

							// Blurred HSV
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_hsv_pixel[0];
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_hsv_pixel[1];
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_hsv_pixel[2];

							// Blurred CIELab
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_cielab_pixel[0];
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_cielab_pixel[1];
							sample_matrix->at<uchar>(sample_index, target_col++) = blurred_cielab_pixel[2];
						}
					}
				}
			});
		});

		cout << "Finished sampling " << training_image_name << endl;
		++current_test_data_number;
		training_image_name = MakeTrainingImageName(current_test_data_number);
	}

	cout << "Final index was " << to_string(current_sample_index.load()) << endl;
}

int main()
{
	// Enter the number of clusters to use.
	cout << "Enter the minimum number of clusters to use (minimum of 1)." << endl;
	min_cluster_count = GetInputInteger();
	while (min_cluster_count < 1)
	{
		cout << "The number must be larger than 0." << endl;
		min_cluster_count = GetInputInteger();
	}
	cout << "Enter the maximum number of clusters to use." << endl;
	max_cluster_count = GetInputInteger();
	while (max_cluster_count < min_cluster_count)
	{
		cout << "The maximum number of clusters must be higher or equal to the minimum number." << endl;
		max_cluster_count = GetInputInteger();
	}

	// Choose whether to use surrounding values.
	char choice = ' ';
	while (choice != 'y' && choice != 'n')
	{
		cout << "Do you wish to use surrounding pixel values? (y/n)" << endl;
		choice = GetInputCharAsLowerCase();
	}
	use_surrounding_values = choice == 'y';
	// If using surrounding values, choose the averaging kernel size
	if (use_surrounding_values)
	{
		cout << "Input the size of the averaging kernel.\nHas to be an odd number.\nMinimum size of 3." << endl;
		int k_size = GetInputInteger();
		while (k_size % 2 == 0 || k_size < 3)
		{
			cout << "Invalid size.\nHas to be an odd number.\nMinimum size of 3." << endl;
			k_size = GetInputInteger();
		}
	}
	// If using surrounding values, choose whether to use ground truth errosion
	if (use_surrounding_values)
	{
		choice = ' ';
		while (choice != 'y' && choice != 'n')
		{
			cout << "Do you wish to use ground truth errosion? (y/n)" << endl;
			choice = GetInputCharAsLowerCase();
		}
		use_errosion = choice == 'y';
	}
	else
	{
		use_errosion = false;
	}

	size_t sample_amount = CountNumberOfSamples();
	int sample_dimension = use_surrounding_values ? 2 * BASE_SAMPLE_DIMENSION : BASE_SAMPLE_DIMENSION;
	Mat sample_matrix(Size(sample_dimension, sample_amount), CV_8U);
	cout << "Starting sampling." << endl;
	BuildSampleMatrix(&sample_matrix);
	cout << "Number of samples collected: " << to_string(sample_matrix.rows) << endl;
	cout << "Finished building sample matrix. Releasing resources." << endl;

	for (int c_count = min_cluster_count; c_count <= max_cluster_count; ++c_count)
	{
		ofstream result_file;
		result_file.open(MakeResultFileName(c_count));

		// Write the sample dimensionality and number of clusters to file
		result_file << to_string(sample_dimension) << ";";
		result_file << to_string(c_count) << ";";

		vector<Mat> clusters;
		if (c_count > 1)
		{
			/*---------------------------------------------------------- K-MEANS -------------------------------------------------------------------------*/
			// Perform k-means clustering on samples
			cout << "Starting k-means clustering." << endl;
			Mat cluster_indices, cluster_centers;
			TermCriteria criteria(TermCriteria::COUNT + TermCriteria::EPS, CLUSTERING_ITERATIONS, CLUSTERING_EPSILON);
			// Make sure format is float, as kmeans requires it
			sample_matrix.convertTo(sample_matrix, CV_32F);
			double compactness = kmeans(sample_matrix, c_count, cluster_indices, criteria, CLUSTERING_ATTEMPTS, KMEANS_PP_CENTERS, cluster_centers);
			cout << "Finished clustering with compactness: " << to_string(compactness) << endl;

			// Convert to double for calculating Mahalanobis distances
			sample_matrix.convertTo(sample_matrix, CV_64F);

			cout << "Processing each cluster in turn." << endl;
			// Separate samples into separate matrices to enable calculations
			// Calculate how many entries each cluster has, so that we can create proper sized Mats
			vector<uint> entry_counts;
			for (uint i = 0; i < c_count; ++i)
			{
				entry_counts.push_back(0);
			}
			uint cluster_index;
			for (int i = 0; i < cluster_indices.rows; ++i)
			{
				cluster_index = cluster_indices.at<uint>(i, 0);
				entry_counts[cluster_index] += 1;
			}

			// Each cluster is represented by a Mat of samples. Create the needed Mats
			for (uint i = 0; i < c_count; ++i)
			{
				clusters.push_back(Mat(Size(sample_dimension, entry_counts[i]), CV_64F));
			}

			// Create array that tracks which row each cluster is currently on
			vector<uint> current_row;
			for (uint i = 0; i < c_count; ++i)
			{
				current_row.push_back(0);
			}
			// Insert samples into correct clusters
			for (int row = 0; row < sample_matrix.rows; ++row)
			{
				uint cluster = cluster_indices.at<uint>(row, 0);
				sample_matrix.row(row).copyTo(clusters[cluster].row(current_row[cluster]));
				current_row[cluster] += 1;
			}
			/*----------------------------------------------------------- END K-MEANS ---------------------------------------------------------------------*/
		}
		else
		{
			sample_matrix.convertTo(sample_matrix, CV_64F);
			clusters.push_back(sample_matrix);
		}

		// Process each cluster
		for (int i = 0; i < c_count; ++i)
		{
			cout << "Processing cluster " << to_string(i) << endl;
			// Calculate Mahalanobis distance for each sample in the cluster
			Mat cluster = clusters[i];
			Mat mean(Size(sample_dimension, 1), CV_64F);
			Mat covar(Size(sample_dimension, sample_dimension), CV_64F);
			calcCovarMatrix(cluster, covar, mean, CV_COVAR_NORMAL | CV_COVAR_ROWS, CV_64F);
			covar = covar / (sample_amount - 1);
			Mat inv_covar;
			invert(covar, inv_covar, DECOMP_SVD);
			Mat mahalanobis_distances(Size(1, cluster.rows), CV_64F);
			for (int row = 0; row < cluster.rows; ++row)
			{
				Mat current_sample = cluster.row(row);
				double mah_dist = Mahalanobis(current_sample, mean, inv_covar);
				mahalanobis_distances.at<double>(row, 0) = mah_dist;
			}

			// Calculate the mean and standard deviation of the Mahalanobis distances
			Mat mah_mean_mat, mah_std_dev_mat;
			meanStdDev(mahalanobis_distances, mah_mean_mat, mah_std_dev_mat);
			double mah_mean, mah_std_dev;
			mah_mean = mah_mean_mat.at<double>(0, 0);
			mah_std_dev = mah_std_dev_mat.at<double>(0, 0);
			cout << "Mean for current cluster: " << to_string(mah_mean) << endl;
			cout << "Standard deviation for current cluster: " << to_string(mah_std_dev) << endl;

			// Write cluster data to file
			cout << "Writing cluster data to file." << endl;
			// Write the Mahalanobis mean to file
			result_file << to_string(mah_mean) << ";";
			// Write the Mahalanobis standard deviation to file
			result_file << to_string(mah_std_dev) << ";";
			// Write the mean vector to file
			for (int i = 0; i < sample_dimension; ++i)
			{
				if (i > 0)
				{
					result_file << ",";
				}
				result_file << to_string(mean.at<double>(0, i));
			}
			result_file << ";";
			// Write the inverse covariance matrix to file
			for (int row = 0; row < sample_dimension; ++row)
			{
				for (int col = 0; col < sample_dimension; ++col)
				{
					if (row > 0 || col > 0)
					{
						result_file << ",";
					}
					result_file << to_string(inv_covar.at<double>(row, col));
				}
			}
			if (i != c_count - 1)
			{
				result_file << ";";
			}
		}

		// Close file
		result_file.close();
	}

	cout << "All data written to file. Training completed successfully." << endl;

	exit(EXIT_SUCCESS);
}

