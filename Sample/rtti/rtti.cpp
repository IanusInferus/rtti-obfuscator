#include <typeinfo>
#include <cstdio>
#include <unordered_map>
#include <string>
#include <memory>
#include <string.h>

namespace com
{
    namespace test_namespace
    {

        class TestClassA
        {
        public:
            virtual ~TestClassA() {};
        };

    }
}

int main(int argc, char** argv)
{
    auto o = std::make_shared<com::test_namespace::TestClassA>();
    auto p = o.get();

    auto cName = typeid(*p).name();
    std::printf("%s\n", cName); //display C05A4ABAA_________________________

    auto cppName = std::string(cName);
    std::printf("%s\n", cppName.c_str()); //display C05A4ABAA_______________________AE with -O2 and not patched

    return 0;
}
