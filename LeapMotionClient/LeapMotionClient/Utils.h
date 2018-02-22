#pragma once

#include "Leap.h"
#include "opencv2\core.hpp"

#include <iostream>
#include <string>
#include <stdio.h>
#include <sstream>
#include <string.h>
#include <algorithm>

using namespace std;
using namespace Leap;
using namespace cv;

const float mm_to_m = 0.001f;


string GetInputString()
{
	string input = "";

	getline(cin, input);

	return input;
}

int GetInputInteger()
{
	string input_string = "";
	int input_number = 0;

	while (true)
	{
		getline(cin, input_string);

		stringstream ss(input_string);
		if (ss >> input_number)
		{
			return input_number;
		}

		cout << "You have entered an invalid integer, please try again." << endl;
	}
}

char GetInputCharAsLowerCase()
{
	string input_char_as_string = "";
	getline(cin, input_char_as_string);
	transform(input_char_as_string.begin(), input_char_as_string.end(), input_char_as_string.begin(), tolower);
	return input_char_as_string.data()[0];
}

bool DoRetryBasedOnInput(string retry_string)
{
	char choice = ' ';
	while (choice != 'y' && choice != 'n')
	{
		cout << retry_string << endl;
		choice = GetInputCharAsLowerCase();
	}
	return choice == 'y';
}

string LeapArmToJsonForearm(Arm* arm)
{
	string forearm_string;
	stringstream ss;

	if (!arm->isValid())
	{
		forearm_string = "null";
	}
	else
	{
		// { "wrist_x": 2.2, ....., "elbow_z": 0.3 }
		ss << "{ ";

		// Wrist position
		ss << "\"wrist_x\": " << -(arm->wristPosition().x) * mm_to_m;
		ss << ", \"wrist_y\": " << arm->wristPosition().z * mm_to_m;
		ss << ", \"wrist_z\": " << arm->wristPosition().y * mm_to_m;
		// Forearm direction
		ss << ", \"direction_x\": " << -(arm->direction().x) * mm_to_m;
		ss << ", \"direction_y\": " << arm->direction().z * mm_to_m;
		ss << ", \"direction_z\": " << arm->direction().y * mm_to_m;
		// Elbow position
		ss << ", \"elbow_x\": " << -(arm->elbowPosition().x) * mm_to_m;
		ss << ", \"elbow_y\": " << arm->elbowPosition().z * mm_to_m;
		ss << ", \"elbow_z\": " << arm->elbowPosition().y * mm_to_m;

		ss << " }";

		forearm_string = ss.str();
	}

	return forearm_string;
}

string LeapFingerToJsonFinger(Finger* finger)
{
	string finger_string;
	stringstream ss;

	// { "type": 2, ....., "tip_velocity_z": 0.2 }

	ss << "{ ";

	// Type
	ss << "\"type\": " << finger->type();
	// Direction
	ss << ", \"direction_x\": " << -(finger->direction().x) * mm_to_m;
	ss << ", \"direction_y\": " << finger->direction().z * mm_to_m;
	ss << ", \"direction_z\": " << finger->direction().y * mm_to_m;
	// Is extended
	ss << ", \"is_extended\": " << finger->isExtended();
	// Tip position
	ss << ", \"tip_x\": " << -(finger->tipPosition().x) * mm_to_m;
	ss << ", \"tip_y\": " << finger->tipPosition().z * mm_to_m;
	ss << ", \"tip_z\": " << finger->tipPosition().y * mm_to_m;
	// Stabilized tip position
	ss << ", \"stabilized_tip_x\": " << -(finger->stabilizedTipPosition().x) * mm_to_m;
	ss << ", \"stabilized_tip_y\": " << finger->stabilizedTipPosition().z * mm_to_m;
	ss << ", \"stabilized_tip_z\": " << finger->stabilizedTipPosition().y * mm_to_m;
	// Tip velocity
	ss << ", \"tip_velocity_x\": " << -(finger->tipVelocity().x) * mm_to_m;
	ss << ", \"tip_velocity_y\": " << finger->tipVelocity().z * mm_to_m;
	ss << ", \"tip_velocity_z\": " << finger->tipVelocity().y * mm_to_m;

	ss << " }";

	finger_string = ss.str();
	return finger_string;
}

string LeapHandToJsonHand(Hand* hand)
{
	string hand_string;
	stringstream ss;

	FingerList fingers = hand->fingers();

	// { "palm_x": 32.4, ....., "fingers": [ {.....},.....{.....} ], ....., "pinch_distance": 2.1 }

	ss << "{ ";

	// Palm position
	ss << "\"palm_x\": " << -(hand->palmPosition().x) * mm_to_m;
	ss << ", \"palm_y\": " << hand->palmPosition().z * mm_to_m;
	ss << ", \"palm_z\": " << hand->palmPosition().y * mm_to_m;
	// Stabilized palm position
	ss << ", \"stabilized_palm_x\": " << -(hand->stabilizedPalmPosition().x) * mm_to_m;
	ss << ", \"stabilized_palm_y\": " << hand->stabilizedPalmPosition().z * mm_to_m;
	ss << ", \"stabilized_palm_z\": " << hand->stabilizedPalmPosition().y * mm_to_m;
	// Palm normal
	ss << ", \"palm_normal_x\": " << -(hand->palmNormal().x) * mm_to_m;
	ss << ", \"palm_normal_y\": " << hand->palmNormal().z * mm_to_m;
	ss << ", \"palm_normal_z\": " << hand->palmNormal().y * mm_to_m;
	// Palm velocity
	ss << ", \"palm_velocity_x\": " << -(hand->palmVelocity().x) * mm_to_m;
	ss << ", \"palm_velocity_y\": " << hand->palmVelocity().z * mm_to_m;
	ss << ", \"palm_velocity_z\": " << hand->palmVelocity().y * mm_to_m;
	// Palm to fingers direction
	ss << ", \"palm_to_fingers_x\": " << -(hand->direction().x) * mm_to_m;
	ss << ", \"palm_to_fingers_y\": " << hand->direction().z * mm_to_m;
	ss << ", \"palm_to_fingers_z\": " << hand->direction().y * mm_to_m;
	// Fingers
	ss << ", \"fingers\": [ ";
	int fingers_added = 0;
	for (int i = 0; i < fingers.count(); ++i)
	{
		if (fingers[i].isValid())
		{
			if (fingers_added > 0)
			{
				ss << ", ";
			}
			ss << LeapFingerToJsonFinger(&(fingers[i]));
			++fingers_added;
		}
	}
	ss << " ]";
	// Grab angle
	ss << ", \"grab_angle\": " << hand->grabAngle();
	// Pinch distance
	ss << ", \"pinch_distance\": " << hand->pinchDistance() * mm_to_m;

	ss << " }";

	hand_string = ss.str();

	return hand_string;
}

string LeapHandToJsonArm(Hand* hand)
{
	string arm_string;
	stringstream ss;

	if (!hand->isValid())
	{
		arm_string = "null";
	}
	else
	{
		Arm forearm = hand->arm();

		// { "forearm": {.....}, "hand": {.....} }
		ss << "{ ";

		// Forearm
		ss << "\"forearm\": " << LeapArmToJsonForearm(&forearm);
		// Hand
		ss << ", \"hand\": " << LeapHandToJsonHand(hand);

		ss << " }";

		arm_string = ss.str();
	}

	return arm_string;
}

string LeapFrameToJson(Frame* frame)
{
	string frame_string;
	stringstream ss;
	
	HandList hands = frame->hands();
	Hand left_hand;
	Hand right_hand;

	for (auto it = hands.begin(); it != hands.end(); ++it)
	{
		if ((*it).isLeft())
		{
			left_hand = (*it);
		}
		else if ((*it).isRight())
		{
			right_hand = (*it);
		}
	}


	// { "left_arm": {.....}, "right_arm": {.....} }
	ss << "{ ";

	if (left_hand.isValid())
	{
		// Add the left arm
		ss << "\"left_arm\": ";
		ss << LeapHandToJsonArm(&left_hand);
	}

	// Add comma to to separate left and right hand if we have info for both
	if (left_hand.isValid() && right_hand.isValid())
	{
		ss << ", ";
	}

	if (right_hand.isValid())
	{
		// Add the right arm
		ss << "\"right_arm\": ";
		ss << LeapHandToJsonArm(&right_hand);
	}

	// Close JSON response
	ss << " }";

	frame_string = ss.str();
	return frame_string;
}

/// Takes all the fingertips from a Leap frame, processes them so they can be used for calibration, adds them to output ordered left to right
void ExtractFingertips(Frame* leap_frame, vector<Point3f>* fingertips)
{
	HandList hands = leap_frame->hands();
	Hand left_hand;
	Hand right_hand;

	for (auto it = hands.begin(); it != hands.end(); ++it)
	{
		if ((*it).isLeft())
		{
			left_hand = (*it);
		}
		else if ((*it).isRight())
		{
			right_hand = (*it);
		}
	}

	FingerList left_fingers = left_hand.fingers();
	FingerList right_fingers = right_hand.fingers();
	// Fingers are in order thumb to pinky. For left hand go from index 0 to 4, and reverese for right hand.

	// When mounted on a HMD the Leap motion coordinate system is a right handed system with the z-axis pointing down,
	// the y-axis pointing forward, and the x-axis pointing left. We want to switch to coordinates that line up with the
	// pinhole camera's coordinates, i.e. z-axis pointing forward, y-axis pointing down, and x-axis pointing right.
	// We also multiply with 0.001 to transform the coordinates from millimeter to meter.
	for (int i = 0; i < 5; ++i)
	{
		Finger current = left_fingers[i];
		Vector leap_vec_pos = current.stabilizedTipPosition();
		Point3f tip_position(-leap_vec_pos.x, leap_vec_pos.z, leap_vec_pos.y);
		tip_position *= 0.001f;
		fingertips->push_back(tip_position);
	}for (int i = 4; i >= 0; --i)
	{
		Finger current = right_fingers[i];
		Vector leap_vec_pos = current.stabilizedTipPosition();
		Point3f tip_position(-leap_vec_pos.x, leap_vec_pos.z, leap_vec_pos.y);
		tip_position *= 0.001f;
		fingertips->push_back(tip_position);
	}
}