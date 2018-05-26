#pragma once

#include "opencv2\core.hpp"

#include <iostream>
#include <string>
#include <stdio.h>
#include <sstream>
#include <string.h>
#include <algorithm>

using namespace std;
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