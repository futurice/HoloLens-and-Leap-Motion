#pragma once

#include "Leap.h"
#include "UDPManager.h"

#include <stdio.h>

using namespace std;
using namespace Leap;

class LeapListener : public Listener
{
public:
	
	void SetUdpManager(UDPManager* manager);
	void onConnect(const Controller& controller);
	void onFrame(const Controller& controller);

private:

	UDPManager* udp_manager;
};

void LeapListener::SetUdpManager(UDPManager* manager)
{
	udp_manager = manager;
}

void LeapListener::onConnect(const Controller& controller)
{
	cout << "Leap Motion controller connected" << endl;
}

void LeapListener::onFrame(const Controller& controller)
{
	udp_manager->SendLeapFrame(&controller.frame());
}