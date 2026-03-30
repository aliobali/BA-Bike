#!/bin/bash
# Quick UDP test commands for macOS/Linux
# Use netcat (nc) to send test data to the bike system

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

SPEED_PORT=5006
STEERING_PORT=5005
HOST="127.0.0.1"

echo -e "${BLUE}╔════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    Bike 3.0 - Quick UDP Test Script       ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════╝${NC}\n"

# Function to send UDP
send_udp() {
    local message=$1
    local port=$2
    local description=$3
    
    echo "$message" | nc -u $HOST $port 2>/dev/null
    echo -e "${GREEN}✓${NC} $description: $message"
    sleep 0.1
}

case "${1:-menu}" in
    "speed")
        # Send specific speed value
        # Usage: ./test_udp.sh speed 5.0
        if [ -z "$2" ]; then
            echo "Usage: ./test_udp.sh speed <value>"
            echo "Example: ./test_udp.sh speed 5.0"
            exit 1
        fi
        send_udp "$2" $SPEED_PORT "Speed"
        ;;
        
    "steer")
        # Send specific steering value
        # Usage: ./test_udp.sh steer 0.5
        if [ -z "$2" ]; then
            echo "Usage: ./test_udp.sh steer <value>"
            echo "Example: ./test_udp.sh steer 0.5"
            exit 1
        fi
        send_udp "$2" $STEERING_PORT "Steering"
        ;;
        
    "accel")
        # Test acceleration
        echo -e "${YELLOW}Testing: Acceleration from 0 to 5 m/s${NC}\n"
        for speed in 0 1 2 3 4 5 5 5 4 3 2 1 0; do
            send_udp "$speed" $SPEED_PORT "Speed"
        done
        echo -e "\n${GREEN}✓ Acceleration test complete${NC}"
        ;;
        
    "turn")
        # Test steering
        echo -e "${YELLOW}Testing: Left and right turns${NC}\n"
        for steer in 0 0.2 0.4 0.6 0.4 0.2 0 -0.2 -0.4 -0.6 -0.4 -0.2 0; do
            send_udp "$steer" $STEERING_PORT "Steering"
        done
        echo -e "\n${GREEN}✓ Steering test complete${NC}"
        ;;
        
    "ride")
        # Simulate a complete ride
        echo -e "${YELLOW}Simulating: Complete bike ride${NC}\n"
        echo -e "${BLUE}Phase 1: Acceleration${NC}"
        for speed in 1 2 3 4 5; do
            send_udp "$speed" $SPEED_PORT "Acceleration"
        done
        
        echo -e "\n${BLUE}Phase 2: Cruising with curves${NC}"
        for i in {1..3}; do
            for steer in 0.3 0.5 0.3 0 -0.3 -0.5 -0.3 0; do
                send_udp "5" $SPEED_PORT ""
                send_udp "$steer" $STEERING_PORT "Steering"
            done
        done
        
        echo -e "\n${BLUE}Phase 3: Deceleration${NC}"
        for speed in 4 3 2 1 0; do
            send_udp "$speed" $SPEED_PORT "Deceleration"
        done
        echo -e "\n${GREEN}✓ Ride simulation complete${NC}"
        ;;
        
    "stop")
        # Send stop commands
        echo -e "${YELLOW}Sending STOP commands${NC}\n"
        send_udp "0" $SPEED_PORT "Speed to 0"
        send_udp "0" $STEERING_PORT "Steering to 0"
        echo -e "\n${GREEN}✓ Bike stopped${NC}"
        ;;
        
    *)
        # Show menu
        echo -e "${YELLOW}Quick Commands:${NC}\n"
        echo -e "  ${GREEN}./test_udp.sh speed <value>${NC}    - Send speed (e.g., 5.0)"
        echo -e "  ${GREEN}./test_udp.sh steer <value>${NC}    - Send steering (e.g., 0.5)"
        echo -e "  ${GREEN}./test_udp.sh accel${NC}           - Test acceleration"
        echo -e "  ${GREEN}./test_udp.sh turn${NC}            - Test steering"
        echo -e "  ${GREEN}./test_udp.sh ride${NC}            - Simulate complete ride"
        echo -e "  ${GREEN}./test_udp.sh stop${NC}            - Send stop command"
        echo ""
        echo -e "${YELLOW}Examples:${NC}\n"
        echo -e "  ${GREEN}# Send 5 m/s${NC}"
        echo -e "  ./test_udp.sh speed 5.0\n"
        echo -e "  ${GREEN}# Send 30° steering${NC}"
        echo -e "  ./test_udp.sh steer 0.6\n"
        echo -e "  ${GREEN}# Test full acceleration curve${NC}"
        echo -e "  ./test_udp.sh accel\n"
        echo -e "  ${GREEN}# Run complete bike simulation${NC}"
        echo -e "  ./test_udp.sh ride\n"
        echo -e "${YELLOW}UDP Configuration:${NC}"
        echo -e "  Speed Port:    ${GREEN}$SPEED_PORT${NC}"
        echo -e "  Steering Port: ${GREEN}$STEERING_PORT${NC}"
        echo -e "  Host:          ${GREEN}$HOST${NC}\n"
        echo -e "${YELLOW}Data Format:${NC}"
        echo -e "  Speed:    Plain number (e.g., 5.0) or labeled (speed:5.0)"
        echo -e "  Steering: Normalized [-1.0, 1.0]\n"
        echo -e "${YELLOW}Note:${NC} Make sure Unity is running with BikeUdpReceiver listening"
        ;;
esac
