// LeapMotionClient.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "Utils.h"
#include "Leap.h"
#include "ConnectionManager.h"
#include <thread>
#include "opencv2\core.hpp"
#include "opencv2\highgui.hpp"
#include "opencv2\imgproc\imgproc.hpp"
#include "HandDetector.h"
#include "FingertipDetector.h"
#include <algorithm>

using namespace std;
using namespace Leap;
using namespace cv;

#pragma comment(lib, "Ws2_32.lib")

#define QUIT_INSTRUCTION_STRING			"Press enter to quit."
#define LEAP_INITIALIZING_STRING		"Leap controller initializing."
#define LEAP_INITIALIZATION_DONE_STRING	"Leap controller initialized. Notifying Hololens that client is ready for calibration."
#define STREAMING_DATA_STRING			"Calibration done. Starting data streaming."

ConnectionManager* connection_manager;

void ListenForStopCall(bool* is_streaming)
{
	cin.get();
	*is_streaming = false;
}

void ListenForControlMessages(bool* is_streaming, bool* is_paused)
{
	while (*is_streaming)
	{
		connection_manager->ListenForControlMessage(is_streaming, is_paused);
	}
}

int main()
{
	Controller leap_controller;
	HandDetector hand_detector;
	FingertipDetector fingertip_detector;

	// Set up socket and connection to Hololens
	connection_manager = new ConnectionManager(&leap_controller);
	connection_manager->ConfigureLocalAddressData();
	connection_manager->ConfigureHoloAddressData();
	connection_manager->CreateSockets();
	connection_manager->BindSockets();
	connection_manager->ConnectToHololens();

	// Create a Leap Controller and wait for it to be connected
	cout << LEAP_INITIALIZING_STRING << endl;
	while (!leap_controller.isConnected()) {}
	// Make sure configs are correct
	leap_controller.setPolicy(Controller::POLICY_OPTIMIZE_HMD);
	leap_controller.config().setInt32("tracking_images_mode", 0);
	leap_controller.config().setBool("tracking_processing_auto_flip", true);
	leap_controller.config().setBool("robust_mode_enabled", false);
	leap_controller.config().setBool("avoid_poor_performance", false);
	leap_controller.config().setInt32("background_app_mode", 2);
	leap_controller.setPolicy(Controller::POLICY_BACKGROUND_FRAMES);
	leap_controller.config().setInt32("process_niceness", 9);
	leap_controller.config().save();
	cout << LEAP_INITIALIZATION_DONE_STRING << endl;

	// Notify Hololens that the Leap client is ready to start calibration
	connection_manager->SendReadyForCalibrationMessage();
	connection_manager->ReceiveCalibrationChoice();

	// Start thread monitoring control messages coming from the Hololens
	bool is_streaming = true;
	bool is_paused = false;
	thread control_message_stream(ListenForControlMessages, &is_streaming, &is_paused);

	// Start thread that monitors if the user wants to quit
	thread stop_button_thread(ListenForStopCall, &is_streaming);

	Frame previous_frame = leap_controller.frame();
	Frame current_frame = previous_frame;
	cout << STREAMING_DATA_STRING << endl;
	cout << QUIT_INSTRUCTION_STRING << endl;

	// Main loop that tracks the current frame
	while (is_streaming)
	{
		current_frame = leap_controller.frame();
		if (current_frame.id() != previous_frame.id())
		{
			previous_frame = current_frame;
			if (!is_paused)
			{
				connection_manager->SendLeapFrame(&current_frame);
			}
		}
	}

	connection_manager->DoCleanup(true);
	exit(EXIT_SUCCESS);
}

