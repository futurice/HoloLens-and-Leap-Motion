#pragma once

#include "Leap.h"
#include "Utils.h"
#include "AppMessages.h"
#include "HandDetector.h"
#include "FingertipDetector.h"
#include "opencv2\core.hpp"
#include "LeapToHoloCalibrator.h"

#include <WinSock2.h>
#include <Ws2tcpip.h>
#include <iostream>
#include <stdio.h>

using namespace std;
using namespace cv;

#define LOCAL_TCP_PORT						20000
#define LOCAL_UDP_PORT						20001
#define HOLO_TCP_PORT						6000
#define HOLO_UDP_PORT						6001
#define RECEIVE_BUFFER_LENGTH				1024

string finger_names[] = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

class ConnectionManager
{
public:
	
	ConnectionManager(Controller* lc)
	{
		leap_controller = lc;
		DoWSAStartup();
	}

	~ConnectionManager()
	{
		DoCleanup(true);
	}

	/// Cleans up used resources.
	void DoCleanup(bool close_socket)
	{
		if (close_socket)
		{
			int tcp_result = closesocket(tcp_socket);
			int udp_result = closesocket(udp_socket);
			if (tcp_result == SOCKET_ERROR || udp_result == SOCKET_ERROR)
			{
				cout << CLOSE_SOCKET_FAIL_STRING << WSAGetLastError() << endl;
			}
		}
		WSACleanup();
	}

	/// Start configuring the local socket address data.
	void ConfigureLocalAddressData()
	{
		cout << CONFIGURING_LOCAL_SOCKET_STRING << endl;
		ConfigureAddressData(true, &local_tcp_sockaddr, &local_udp_sockaddr);
	}

	/// Start configuring the Hololens socket address data.
	void ConfigureHoloAddressData()
	{
		cout << CONFIGURING_HOLO_SOCKET_STRING << endl;
		ConfigureAddressData(false, &holo_tcp_sockaddr, &holo_udp_sockaddr);
	}

	/// Create the sockets to be used. If creation fails the user is given the choice to retry.
	void CreateSockets()
	{
		tcp_socket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
		udp_socket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		if (tcp_socket == INVALID_SOCKET || udp_socket == INVALID_SOCKET)
		{
			cout << SOCKET_CREATION_FAIL_STRING << WSAGetLastError() << endl;
			if (DoRetryBasedOnInput(RETRY_SOCKET_CREATION_STRING))
			{
				CreateSockets();
			}
			else
			{
				DoCleanup(false);
				exit(EXIT_FAILURE);
			}
		}
	}

	/// Bind the local addresses to sockets. If binding fails the user can choose to retry.
	void BindSockets()
	{
		if (::bind(tcp_socket, (sockaddr*)&local_tcp_sockaddr, sizeof(local_tcp_sockaddr)) == SOCKET_ERROR || 
			::bind(udp_socket, (sockaddr*)&local_udp_sockaddr, sizeof(local_udp_sockaddr)) == SOCKET_ERROR)
		{
			cout << BIND_SOCKET_FAIL_STRING << WSAGetLastError() << endl;
			if (DoRetryBasedOnInput(RETRY_SOCKET_BIND_STRING))
			{
				BindSockets();
			}
			else
			{
				DoCleanup(true);
				exit(EXIT_FAILURE);
			}
		}
	}

	/// Tries to establish connection to Hololens. If it fails the user can choose to retry.
	void ConnectToHololens()
	{
		if (connect(tcp_socket, (sockaddr*)&holo_tcp_sockaddr, sizeof(holo_tcp_sockaddr)) == SOCKET_ERROR || 
			connect(udp_socket, (sockaddr*)&holo_udp_sockaddr, sizeof(holo_udp_sockaddr)) == SOCKET_ERROR)
		{
			cout << CONNECT_ERROR_STRING << WSAGetLastError() << endl;
			if (DoRetryBasedOnInput(RETRY_CONNECT_STRING))
			{
				ConnectToHololens();
			}
			else
			{
				DoCleanup(true);
				exit(EXIT_FAILURE);
			}
		}
	}

	/// Send message to Hololens that the Leap Motion client is ready for calibration.
	void SendReadyForCalibrationMessage()
	{
		int bytes_sent = send(tcp_socket, LEAP_RUNNING_MESSAGE_STRING, strlen(LEAP_RUNNING_MESSAGE_STRING) * sizeof(char), 0);
		cout << LEAP_RUNNING_MESSAGE_SENT_STRING << bytes_sent << endl;
	}

	/// Wait for message saying if calibration is to be done or a previous result will be used
	void ReceiveCalibrationChoice()
	{
		// Clear buffer and wait to receive instructions
		memset(recv_buffer, '\0', RECEIVE_BUFFER_LENGTH);
		int bytes_received = 0;
		while (bytes_received <= 0)
		{
			bytes_received = recv(tcp_socket, recv_buffer, RECEIVE_BUFFER_LENGTH, 0);
		}
		string choice(recv_buffer);
		cout << choice << endl;
		if (choice == DO_CALIBRATION_STRING)
		{
			ReceiveCalibrationMessage();
		}
	}

	/// Wait for the Hololens to send calibration message.
	void ReceiveCalibrationMessage()
	{
		// First receive message with fx, fy, cx, cy, image width and height, number of images that will be 
		// sent, and the size in bytes of each image
		memset(recv_buffer, '\0', RECEIVE_BUFFER_LENGTH);
		int bytes_received = 0;
		while (bytes_received <= 0)
		{
			bytes_received = recv(tcp_socket, recv_buffer, RECEIVE_BUFFER_LENGTH, 0);
		}

		// Read the received message
		string received_message(recv_buffer);
		string fx_str, fy_str, cx_str, cy_str, width_str, height_str, image_amount_str, image_size_str;
		float fx, fy, cx, cy;
		int width, height, image_amount, image_size;
		stringstream ss(received_message);
		getline(ss, fx_str, ';');
		getline(ss, fy_str, ';');
		getline(ss, cx_str, ';');
		getline(ss, cy_str, ';');
		getline(ss, width_str, ';');
		getline(ss, height_str, ';');
		getline(ss, image_amount_str, ';');
		getline(ss, image_size_str, ';');
		fx = stof(fx_str);
		fy = stof(fy_str);
		cx = stof(cx_str);
		cy = stof(cy_str);
		width = stoi(width_str);
		height = stoi(height_str);
		image_amount = stoi(image_amount_str);
		image_size = stoi(image_size_str);

		// Start receiving images
		int number_of_images_received = 0;
		vector<Mat> received_images;
		vector<Frame> leap_frames;
		do
		{
			// Clear buffer and start receiving the image
			bool frame_captured = false;
			memset(recv_buffer, '\0', RECEIVE_BUFFER_LENGTH);
			char* image_data = new char[image_size];
			bytes_received = 0;
			int total_bytes_received = 0;
			do
			{
				bytes_received = recv(tcp_socket, recv_buffer, RECEIVE_BUFFER_LENGTH, 0);
				copy(recv_buffer, recv_buffer + bytes_received, image_data + total_bytes_received);
				total_bytes_received += bytes_received;
				if (!frame_captured && total_bytes_received > 0)
				{
					leap_frames.push_back(leap_controller->frame());
					frame_captured = true;
				}
			} 
			while (total_bytes_received < image_size);

			// Convert received image data into a Mat for future processing
			Mat calibration_image(Size(width, height), CV_8UC3);
			for (int row = 0; row < height; ++row)
			{
				for (int col = 0; col < width; ++col)
				{
					int index = row * width * 3 * sizeof(char) + col * 3 * sizeof(char);
					Vec3b pix;
					pix[0] = (uchar)image_data[index];
					pix[1] = (uchar)image_data[index + 1];
					pix[2] = (uchar)image_data[index + 2];
					calibration_image.at<Vec3b>(row, col) = pix;
				}
			}
			received_images.push_back(calibration_image);

			delete[] image_data;
			image_data = NULL;

			number_of_images_received++;
		} 
		while (number_of_images_received < image_amount);

		// Find the fingertips in each image and each Leap frame
		vector<Point2f> image_fingertips;
		vector<Point3f> leap_fingertips;
		for (int i = 0; i < image_amount; ++i)
		{
			// Image
			Mat current_img = received_images[i];
			Mat hand_image = hand_detector.DetectHands(&current_img, true);
			fingertip_detector.FindFingertips(&hand_image, &image_fingertips);
			
			// Leap frame
			ExtractFingertips(&leap_frames[i], &leap_fingertips);
		}

		Mat rot_mat(3, 3, CV_64F);
		Mat trans_vec(3, 1, CV_64F);
		calibrator.Calibrate(&rot_mat, &trans_vec, fx, fy, cx, cy, &image_fingertips, &leap_fingertips);

		// Send the result of the calibration to the Hololens
		string transform_string = string(LEAP_CALIBRATION_SUCCESS_STRING);
		for (int row = 0; row < 3; ++row)
		{
			for (int col = 0; col < 3; ++col)
			{
				transform_string += to_string(rot_mat.at<double>(row, col)) + ";";
			}
		}
		transform_string += to_string(trans_vec.at<double>(0, 0)) + ";";
		transform_string += to_string(trans_vec.at<double>(1, 0)) + ";";
		transform_string += to_string(trans_vec.at<double>(2, 0));
		transform_string += "\n";
		const char* message = transform_string.c_str();
		int bytes_sent = send(tcp_socket, message, strlen(message) * sizeof(char), 0);
		cout << "Sent result of calibration. Bytes sent: " << bytes_sent << endl;

		ListenForCalibrationResult();
	}

	void ListenForCalibrationResult()
	{
		memset(recv_buffer, '\0', RECEIVE_BUFFER_LENGTH);
		int bytes_received = 0;
		while (bytes_received <= 0)
		{
			bytes_received = recv(tcp_socket, recv_buffer, RECEIVE_BUFFER_LENGTH, 0);
		}
		// Read the received message
		string received_message(recv_buffer);
		if (received_message != HOLO_CALIBRATION_SUCCESS_STRING)
		{
			if (received_message == HOLO_CALIBRATION_FAIL_STRING)
			{
				SendReadyForCalibrationMessage();
				ReceiveCalibrationMessage();
			}
			else
			{
				DoCleanup(true);
				exit(EXIT_SUCCESS);
			}
		}
	}

	/// Send the relevant info of a frame as JSON
	void SendLeapFrame(Leap::Frame* frame)
	{
		string frame_json_string = LeapFrameToJson(frame);
		size_t string_length = frame_json_string.size();
		send(udp_socket, frame_json_string.c_str(), string_length * sizeof(char), 0);
	}

	/// Listen for control messages from the Hololens
	void ListenForControlMessage(bool* is_streaming, bool* is_paused)
	{
		// Clear buffer
		memset(recv_buffer, '\0', RECEIVE_BUFFER_LENGTH);
		int bytes_received = 0;
		while (bytes_received <= 0 && *is_streaming)
		{
			bytes_received = recv(tcp_socket, recv_buffer, RECEIVE_BUFFER_LENGTH, 0);
		}
		string message(recv_buffer);
		cout << message << endl;
		if (message == PAUSE_STREAMING_STRING)
		{
			*is_paused = true;
		}
		else if (message == RESUME_STREAMING_STRING)
		{
			*is_paused = false;
		}
		else if (message == END_STREAMING_STRING)
		{
			*is_streaming = false;
		}
	}

private:

	/// Intializes Winsocket. If the initialization fails the user given the choice of trying again.
	int DoWSAStartup()
	{
		int result = WSAStartup(MAKEWORD(2, 2), &wsa_data);

		if (result != 0)
		{
			if (DoRetryBasedOnInput(WSA_STARTUP_FAILED_STRING))
			{
				DoWSAStartup();
			}
			else
			{ 
				exit(EXIT_FAILURE);
			}
		}
	}

	/// Configure the given socket address data using user given IP address and port number.
	void ConfigureAddressData(bool is_local, sockaddr_in *tcp_data, sockaddr_in *udp_data)
	{
		tcp_data->sin_family = AF_INET;
		udp_data->sin_family = AF_INET;
		cout << ENTER_IP_STRING << endl;
		string ip = GetInputString();
		tcp_data->sin_port = is_local ? htons(LOCAL_TCP_PORT) : htons(HOLO_TCP_PORT);
		udp_data->sin_port = is_local ? htons(LOCAL_UDP_PORT) : htons(HOLO_UDP_PORT);
		if (inet_pton(AF_INET, ip.c_str(), &tcp_data->sin_addr) != 1 || inet_pton(AF_INET, ip.c_str(), &udp_data->sin_addr) != 1)
		{
			cout << INVALID_INPUT_STRING << endl;
			ConfigureAddressData(is_local, tcp_data, udp_data);
		}

		cout << endl;
	}

	WSADATA					wsa_data;
	SOCKET					tcp_socket;
	SOCKET					udp_socket;
	sockaddr_in				local_tcp_sockaddr;
	sockaddr_in				local_udp_sockaddr;
	sockaddr_in				holo_tcp_sockaddr;
	sockaddr_in				holo_udp_sockaddr;

	Controller*				leap_controller;
	HandDetector			hand_detector;
	FingertipDetector		fingertip_detector;
	LeapToHoloCalibrator	calibrator;


	char						recv_buffer[RECEIVE_BUFFER_LENGTH];
};