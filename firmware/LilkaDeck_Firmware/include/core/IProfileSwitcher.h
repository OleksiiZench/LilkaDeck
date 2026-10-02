#pragma once

class IProfileSwitcher {
public:
    virtual ~IProfileSwitcher() = default;

    virtual void nextProfile() = 0;
    virtual void previousProfile() = 0;
};
