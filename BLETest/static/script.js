console.log("JS connected!")

async function connectToController() {
    try {
        console.log('Searching for GameController...');

        const device = await navigator.bluetooth.requestDevice({
        filters: [
            { name: 'GameController' } // Name of the bluetooth device
        ],
        optionalServices: ['4fafc201-1fb5-459e-8fcc-c5c9c331914b'] // enable read/write (UUID), (Needs to match controller)
        });

        console.log('Found it!', device.name);

        const server = await device.gatt.connect();

        const service = await server.getPrimaryService('4fafc201-1fb5-459e-8fcc-c5c9c331914b');
        
        const characteristic = await service.getCharacteristic('beb5483e-36e1-4688-b7f5-ea07361b26a8');

        console.log('Ready to receive game data!');

    } catch (error) {
        console.error('Connection failed:', error);
    }
}