#include <windows.h>
#include <iostream>
#include <fstream>
#pragma comment(lib, "winmm.lib")

using namespace std;

#define MAX_BUFFER_SIZE (88200)
#define MAX_BUFFER_COUNT 2
typedef struct {
	char RIFF[4]; // 'RIFF'
	unsigned long ChunkSize; //ChunkSize
	char WAVE[4]; // 'WAVE'
	char fmt[4]; // 'fmt '
	unsigned long SubChunk1Size; // Subchunk1Size
	unsigned short AudioFormat; // AudioFomat = 1 (PCM)
	unsigned short NumberChannels; // NumberChannels
	unsigned long SamplesPerSec; // SampleRate
	unsigned long BytesPerSec; // ByteRate
	unsigned short BlockAlign;  // BlockAlign
	unsigned short BitsPerSample; // BitsPerSample
	char SubChunk2ID[4]; // 'data'
	unsigned long Subchunk2Size; //Subchunk2Size
}WAV_HEADER;

class Player {
public:
	int           t_sec;
	int           t_min;
	double        percent;
	int           c_sec;
	int           c_min;

private:
	HWAVEOUT      hwo;
	WAVEFORMATEX  wfx;	
	HANDLE        wait;
	WAVEHDR       header[2];
	WAV_HEADER    wavHeader;
	DWORD         play_volume = 0xFFFFFFFF;
	char*         buffer;
	int           pointer;
	int           index;
	bool          key;
	bool          play_pause = false;
	bool          play_restart = false;
	bool          play_setVolume = false;
	bool          play_drag = false;
	int           offset;

public:
	Player();
	~Player();
	void setVolume(double percent);
	void setProgress(double progress);
	void pause();
	void restart();	
	void start(const char* filePath);

private:
	int play(const char* buf, int size);
	int open();
	void close();
	int write(const char* buf, int size);
	int flush();
};

Player::Player() {
	hwo = NULL;
	wait = CreateEvent(NULL, FALSE, FALSE, NULL);
	for (int i = 0; i < MAX_BUFFER_COUNT; i++) {
		header[i].dwFlags = 0;
		header[i].lpData = new char[MAX_BUFFER_SIZE];
		header[i].dwBufferLength = MAX_BUFFER_SIZE;
	}
	buffer = new char[MAX_BUFFER_SIZE];
}

Player::~Player() {
	close();
	CloseHandle(wait);
	wait = NULL;
}

int Player::open() {
	WAVEFORMATEX wfx;

	// load waveformat data
	wfx.wFormatTag = WAVE_FORMAT_PCM;
	wfx.nChannels = wavHeader.NumberChannels; // channels
	wfx.nSamplesPerSec = wavHeader.SamplesPerSec; // sample rate	
	wfx.wBitsPerSample = wavHeader.BitsPerSample; // sample size		
	wfx.nBlockAlign = (wfx.wBitsPerSample / 8) * wfx.nChannels;
	wfx.nAvgBytesPerSec = wfx.nBlockAlign * wfx.nSamplesPerSec;
	wfx.cbSize = sizeof(wfx); // size of extra information.

	// open device
	if (waveOutOpen(&hwo, WAVE_MAPPER, &wfx, (DWORD_PTR)wait, 0, CALLBACK_EVENT) != MMSYSERR_NOERROR)
	{
		return -1;
	}
	
	// prepare header
	for (int i = 0; i < MAX_BUFFER_COUNT; i++) {
		waveOutPrepareHeader(hwo, &header[i], sizeof(WAVEHDR)); 
		if (!(header[i].dwFlags & WHDR_PREPARED))
			return -1;
	}

	pointer = 0;
	index = 0;
	key = 0;

	return 0;
}

void Player::close() {
	if (hwo != NULL) {
		for (int i = 0; i < MAX_BUFFER_COUNT; i++) {
			waveOutUnprepareHeader(hwo, &header[i], sizeof(WAVEHDR));
			delete[] header[i].lpData;
		}	
	delete buffer;
	waveOutClose(hwo);
	hwo = NULL;
	}
}

// two buffer, while one buffer is being read, write to another one.
int Player::write(const char* buf, int size) {
	do {
		// if the buffer is still containable
		if (pointer + size < MAX_BUFFER_SIZE) {
			memcpy(buffer + pointer, buf, size);
			pointer += size;
		}
		else {
			// fill the buffer
			memcpy(buffer + pointer, buf, MAX_BUFFER_SIZE - pointer);

			//
			if (!key) {
				if (index == 0) {
					memcpy(header[0].lpData, buffer, MAX_BUFFER_SIZE);
					index = 1;
				}
				else {
					ResetEvent(wait);
					memcpy(header[1].lpData, buffer, MAX_BUFFER_SIZE);

					waveOutWrite(hwo, &header[0], sizeof(WAVEHDR));
					waveOutWrite(hwo, &header[1], sizeof(WAVEHDR));

					key = 1;
					index = 0;
				}
			}
			// play
			else if (play(buffer, MAX_BUFFER_SIZE) < 0) {
				return -1;
			}
			percent += double(MAX_BUFFER_SIZE) / wavHeader.ChunkSize;
			c_sec = int((60 * t_min + t_sec)*percent) % 60;
			c_min = int((60 * t_min + t_sec)*percent) / 60;
			size -= MAX_BUFFER_SIZE - pointer;
			buf += MAX_BUFFER_SIZE - pointer;
			pointer = 0;

			if (size > 0)
				continue;
			else
				break;
		}
	} while (0);
	return 0;
}

int Player::flush() {
	if (pointer > 0 && play(buffer, pointer) < 0) {
		return -1;
	}
	return 0;
}

int Player::play(const char* buf, int size) {
	WaitForSingleObject(wait, INFINITE);
	header[index].dwBufferLength = size;
	memcpy(header[index].lpData, buf, size);

	if (waveOutWrite(hwo, &header[index], sizeof(WAVEHDR))) {
		SetEvent(wait);
		return -1;
	}
	index = !index;
	return 0;
}

void Player::start(const char* filePath) {
	char* readBuffer = new char[1024 * 8];
	std::ifstream wavFile(filePath, std::ifstream::binary);
	if (!wavFile) {
		cout << "failed to open file.\n";
		return;
	}
	// move pointer to the end of file
	wavFile.seekg(0, wavFile.end);
	int fileSize = wavFile.tellg();
	// move pointer back
	wavFile.seekg(0, wavFile.beg);
	// read .wav header
	wavFile.read((char*)&wavHeader, sizeof(WAV_HEADER));

	cout << "File is:                    " << fileSize << " bytes." << endl;
	cout << "RIFF header:                " << wavHeader.RIFF[0]
		<< wavHeader.RIFF[1]
		<< wavHeader.RIFF[2]
		<< wavHeader.RIFF[3] << endl;
	cout << "WAVE header:                " << wavHeader.WAVE[0]
		<< wavHeader.WAVE[1]
		<< wavHeader.WAVE[2]
		<< wavHeader.WAVE[3]
		<< endl;
	cout << "FMT:                        " << wavHeader.fmt[0]
		<< wavHeader.fmt[1]
		<< wavHeader.fmt[2]
		<< wavHeader.fmt[3]
		<< endl;
	cout << "Data size:                  " << wavHeader.ChunkSize << " bytes." << endl;
	cout << "Sampling Rate:              " << wavHeader.SamplesPerSec << endl;
	cout << "Number of bits per sample:  " << wavHeader.BitsPerSample << endl;
	cout << "Number of channels:         " << wavHeader.NumberChannels << endl;
	cout << "Number of bytes per second: " << wavHeader.BytesPerSec << " bytes." << endl;
	cout << "Data length:                " << wavHeader.Subchunk2Size << " bytes." << endl;
	cout << "Audio Format:               " << wavHeader.AudioFormat << endl;
	cout << "Block align:                " << wavHeader.BlockAlign << endl;
	cout << "Data string:                " << wavHeader.SubChunk2ID[0]
		<< wavHeader.SubChunk2ID[1]
		<< wavHeader.SubChunk2ID[2]
		<< wavHeader.SubChunk2ID[3]
		<< endl;

	t_sec = wavHeader.ChunkSize / wavHeader.BytesPerSec % 60;
	t_min = wavHeader.ChunkSize / wavHeader.BytesPerSec / 60;
	cout << "Time:                       " << t_min << "min" << t_sec << "sec" << endl;
	if (open() < 0) {
		std::cout << "waveout open failed.\n";
		return;
	}
	waveOutSetVolume(hwo, play_volume);

	while (1) {
		if (play_pause) {
			waveOutPause(hwo);
			continue;
		}
		else if (play_restart) {
			waveOutRestart(hwo);
			play_restart = false;
		}
		if (play_setVolume) {
			waveOutSetVolume(hwo, play_volume);
			play_setVolume = false;
		}
		if (play_drag) {
			wavFile.seekg(offset);
			play_drag = false;
		}
		if (wavFile.read(readBuffer, sizeof(readBuffer))) {
			if (write(readBuffer, sizeof(readBuffer)) < 0)
				cout << "play failed.\n";
		}
		else {
			if (flush() < 0)
				cout << "flush failed\n";
			cout << "play done.\n";
			delete[] readBuffer;
			//close();
			break;
		}
	}
}
void Player::setVolume(double percent) {
	DWORD volume = unsigned int(0xFFFF * percent);

	play_setVolume = true;
	play_volume = (volume<<16) + volume;
}

void Player::setProgress(double progress) {
	percent = progress;
	offset = int((60 * t_min + t_sec)*percent) * wavHeader.BytesPerSec + 44;
	cout << "offset" << offset << endl;
	play_drag = true;
	cout << "play_drag" << play_drag << endl;
}
void Player::pause() {
	play_pause = true;
	play_restart = false;
}

void Player::restart() {
	play_restart = true;
	play_pause = false;
}
