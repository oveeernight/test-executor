#ifndef LOGGING_H_
#define LOGGING_H_

#ifdef _CYGWIN
#undef max
#undef min
#endif
#include <fstream>
#include <mutex>
#include <thread>

#define HEX(x) std::hex << "0x" << (x) << std::dec

#ifdef _LOGGING
extern std::ofstream tout;
extern std::recursive_mutex logMutex;

#define LOG_CODE(CODE) { CODE } ((void) 0)

void open_log(const char*& logName);
void close_log();


#else
#define LOG_CODE(CODE) ((void) 0)

static inline void open_log() {}
static inline void close_log() {}
#endif

#define LOG(CODE) LOG_CODE(logMutex.lock(); tout << "[" << std::this_thread::get_id() << "] "; CODE ; tout << "\n"; tout.flush(); logMutex.unlock();)
#define LOG_ERROR(CODE) LOG_CODE(tout << "-------- [LOG_ERROR] " << __FUNCTION__ << " " << __FILE__ << ":" << __LINE__ << " ---------\n"; CODE ; tout << "------------------------------------------------\n"; tout.flush();)


#endif // LOGGING_H_
